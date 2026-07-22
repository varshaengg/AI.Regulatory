using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>
/// The persona × feature × permission matrix (A6 grid). The default seed
/// mirrors <c>Script.PostDeployment.sql</c> so mocked mode gives the same
/// answers as a fresh SQL DB.
/// </summary>
/// <remarks>
/// The Admin verb is stored implicitly — the seed for the Admin persona lists
/// <c>Admin</c> against every feature and <see cref="GetEffectivePermissions"/>
/// expands that to Read + Write + Review + Admin at read time.
/// </remarks>
public sealed class PermissionMatrixRepository : BaseRepository<PermissionMatrixEntry>
{
    private static readonly string[] AdminImplies = { "Read", "Write", "Review", "Admin" };

    private readonly ISqlConnectionFactory _sql;

    public PermissionMatrixRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) { _sql = sql; }

    protected override bool MatchesId(PermissionMatrixEntry item, string id) => false;

    protected override IEnumerable<PermissionMatrixEntry> SeedData()
    {
        // Encoded as tuples for readability; MERGE below flattens into the store.
        (string Persona, string Feature, string Permission)[] grants =
        [
            // Admin persona — full access to every feature.
            ("Admin", "UserManagement",    "Admin"),
            ("Admin", "DossierManagement", "Admin"),
            ("Admin", "Templates",         "Admin"),
            ("Admin", "Assignments",       "Admin"),
            ("Admin", "Reviews",           "Admin"),
            ("Admin", "Notifications",     "Admin"),
            // RA Lead
            ("RaLead", "DossierManagement", "Read"),
            ("RaLead", "DossierManagement", "Write"),
            ("RaLead", "Templates",         "Read"),
            ("RaLead", "Templates",         "Write"),
            ("RaLead", "Assignments",       "Read"),
            ("RaLead", "Assignments",       "Write"),
            ("RaLead", "Reviews",           "Read"),
            ("RaLead", "Notifications",     "Read"),
            // RA Author
            ("RaAuthor", "DossierManagement", "Read"),
            ("RaAuthor", "DossierManagement", "Write"),
            ("RaAuthor", "Templates",         "Read"),
            ("RaAuthor", "Assignments",       "Read"),
            ("RaAuthor", "Assignments",       "Write"),
            ("RaAuthor", "Notifications",     "Read"),
            // RA Reviewer — the persona that adds 'Review' on top of Read/Write.
            ("RaReviewer", "DossierManagement", "Read"),
            ("RaReviewer", "DossierManagement", "Review"),
            ("RaReviewer", "Templates",         "Read"),
            ("RaReviewer", "Assignments",       "Read"),
            ("RaReviewer", "Reviews",           "Read"),
            ("RaReviewer", "Reviews",           "Write"),
            ("RaReviewer", "Reviews",           "Review"),
            ("RaReviewer", "Notifications",     "Read"),
        ];
        return grants.Select(g => new PermissionMatrixEntry(g.Persona, g.Feature, g.Permission, true));
    }

    /// <summary>Full matrix as a list; store update is a MERGE by (persona,feature,permission).</summary>
    public Task<IReadOnlyList<PermissionMatrixEntry>> GetMatrix(CancellationToken ct = default)
        => ListAsync(ct);

    /// <summary>Toggle a single cell. In mocked mode mutates the seed; live mode goes through the store hook.</summary>
    public async Task<PermissionMatrixEntry> Upsert(UpdatePermissionRequest req, CancellationToken ct)
    {
        var entry = new PermissionMatrixEntry(req.PersonaCode, req.FeatureCode, req.PermissionCode, req.Granted);
        if (IsMocked)
        {
            SeedList.RemoveAll(x => x.PersonaCode.Equals(req.PersonaCode, StringComparison.OrdinalIgnoreCase)
                                 && x.FeatureCode.Equals(req.FeatureCode, StringComparison.OrdinalIgnoreCase)
                                 && x.PermissionCode.Equals(req.PermissionCode, StringComparison.OrdinalIgnoreCase));
            if (req.Granted) SeedList.Add(entry);
            return entry;
        }

        // Live path — lookup ids by code, then INSERT or DELETE the join row.
        await using var c = await _sql.OpenAsync(ct);
        const string LookupSql = @"
            SELECT
                (SELECT [Id] FROM [dbo].[Persona]    WHERE [Code] = @Persona)    AS PersonaId,
                (SELECT [Id] FROM [dbo].[Feature]    WHERE [Code] = @Feature)    AS FeatureId,
                (SELECT [Id] FROM [dbo].[Permission] WHERE [Code] = @Permission) AS PermissionId;";
        var ids = await c.QuerySingleAsync<(int? PersonaId, int? FeatureId, int? PermissionId)>(
            new CommandDefinition(LookupSql,
                new { Persona = req.PersonaCode, Feature = req.FeatureCode, Permission = req.PermissionCode },
                cancellationToken: ct));

        if (ids.PersonaId is null || ids.FeatureId is null || ids.PermissionId is null)
            throw new InvalidOperationException(
                $"Unknown code(s): persona={req.PersonaCode}, feature={req.FeatureCode}, permission={req.PermissionCode}.");

        if (req.Granted)
        {
            const string InsertSql = @"
                IF NOT EXISTS (SELECT 1 FROM [dbo].[PersonaPermission]
                               WHERE [PersonaId]=@PersonaId AND [FeatureId]=@FeatureId AND [PermissionId]=@PermissionId)
                    INSERT INTO [dbo].[PersonaPermission] ([PersonaId],[FeatureId],[PermissionId])
                    VALUES (@PersonaId,@FeatureId,@PermissionId);";
            await c.ExecuteAsync(new CommandDefinition(InsertSql, ids, cancellationToken: ct));
        }
        else
        {
            const string DeleteSql = @"
                DELETE FROM [dbo].[PersonaPermission]
                WHERE [PersonaId]=@PersonaId AND [FeatureId]=@FeatureId AND [PermissionId]=@PermissionId;";
            await c.ExecuteAsync(new CommandDefinition(DeleteSql, ids, cancellationToken: ct));
        }
        return entry;
    }

    /// <summary>
    /// Reduce persona list → effective feature → verb set. Admin persona
    /// implicitly gets Read+Write+Review+Admin on every feature it holds.
    /// </summary>
    public async Task<IReadOnlyList<PermissionGrant>> GetEffectivePermissions(
        IEnumerable<string> personaCodes, CancellationToken ct)
    {
        var matrix = await ListAsync(ct);
        var set = new HashSet<string>(personaCodes ?? Enumerable.Empty<string>(),
                                       StringComparer.OrdinalIgnoreCase);

        var byFeature = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in matrix.Where(r => r.Granted && set.Contains(r.PersonaCode)))
        {
            if (!byFeature.TryGetValue(row.FeatureCode, out var verbs))
                byFeature[row.FeatureCode] = verbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.Equals(row.PermissionCode, "Admin", StringComparison.OrdinalIgnoreCase))
                foreach (var v in AdminImplies) verbs.Add(v);
            else
                verbs.Add(row.PermissionCode);
        }
        return byFeature
            .Select(kv => new PermissionGrant(kv.Key, kv.Value.OrderBy(v => v).ToArray()))
            .OrderBy(g => g.FeatureCode)
            .ToArray();
    }

    protected override async Task<IReadOnlyList<PermissionMatrixEntry>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        const string Sql = @"
            SELECT p.[Code] AS PersonaCode, f.[Code] AS FeatureCode, pm.[Code] AS PermissionCode,
                   CAST(1 AS BIT) AS Granted
            FROM   [dbo].[PersonaPermission] pp
            JOIN   [dbo].[Persona]    p  ON p.[Id]  = pp.[PersonaId]
            JOIN   [dbo].[Feature]    f  ON f.[Id]  = pp.[FeatureId]
            JOIN   [dbo].[Permission] pm ON pm.[Id] = pp.[PermissionId];";
        var rows = await c.QueryAsync<PermissionMatrixEntry>(new CommandDefinition(Sql, cancellationToken: ct));
        return rows.ToArray();
    }

    // Admin implies lower verbs — this expansion happens only at read time so
    // the stored matrix stays sparse.
}
