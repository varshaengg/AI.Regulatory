using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>
/// App users + their persona assignments — A5. Seed contains a handful of
/// representative accounts spanning every persona so the mocked A5 grid is
/// meaningful on first load.
/// </summary>
/// <remarks>
/// SQL storage:
/// <list type="bullet">
///   <item><description><c>dbo.AppUser</c> holds a row per enrolled Entra user. Primary key is <c>Id INT IDENTITY</c>;
///     <c>AadObjectId UNIQUEIDENTIFIER</c> is the natural key from Entra. The <see cref="AppUser.Id"/> string in the
///     contract is <c>Id.ToString()</c> — callers may pass either form (Id or AadObjectId) to lookup methods.</description></item>
///   <item><description><c>dbo.UserPersona</c> is a join table; <c>DELETE ON CASCADE</c> keeps it in sync when a user is removed.</description></item>
/// </list>
/// Enrol is idempotent by AadObjectId (adds any missing persona rows).
/// </remarks>
public sealed class AppUsersRepository : BaseRepository<AppUser>
{
    private readonly ISqlConnectionFactory _sql;

    public AppUsersRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) { _sql = sql; }

    protected override bool MatchesId(AppUser item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)
        || string.Equals(item.AadObjectId, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<AppUser> SeedData() => new[]
    {
        new AppUser("u-sara",   "aad-sara-o",   "Sara O'Neill",   "sara.oneill@contoso.com",  "IT Administrator",   ["Admin"],                DateTime.UtcNow.AddDays(-120), "system"),
        new AppUser("u-marcus", "aad-marcus-h", "Marcus Hollander","marcus.h@contoso.com",    "Regulatory Lead",    ["RaLead"],               DateTime.UtcNow.AddDays(-95),  "sara.oneill@contoso.com"),
        new AppUser("u-priya",  "aad-priya-r",  "Priya Ramanathan","priya.r@contoso.com",     "Regulatory Author",  ["RaAuthor"],             DateTime.UtcNow.AddDays(-60),  "marcus.h@contoso.com"),
        new AppUser("u-chen",   "aad-chen-l",   "Dr. Chen Liu",    "chen.liu@contoso.com",    "Regulatory Reviewer",["RaReviewer"],           DateTime.UtcNow.AddDays(-40),  "marcus.h@contoso.com"),
        new AppUser("u-mika",   "aad-mika-t",   "Mika Tanaka",     "mika.t@contoso.com",      "Sr. Regulatory",     ["RaLead", "RaReviewer"], DateTime.UtcNow.AddDays(-10),  "sara.oneill@contoso.com"),
    };

    // AppUser.JobTitle isn't in SQL; store it in UPN slot? No — we keep it as
    // in-memory only for mocked mode. In live mode we source it from the row
    // insert time (best-effort) and read it back from People-Picker inputs.
    // To avoid data loss on read-back, we synthesize the AppUser record from
    // AppUser + UserPersona rows and use displayName for JobTitle when null.

    /// <summary>Enrol a person previously chosen via the people picker.</summary>
    public async Task<AppUser> Create(CreateAppUserRequest req, string addedBy, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = await GetAsync(req.AadObjectId, ct);
            if (existing is not null)
            {
                var merged = existing with
                {
                    PersonaCodes = existing.PersonaCodes.Union(req.PersonaCodes, StringComparer.OrdinalIgnoreCase).ToArray(),
                };
                return await Replace(merged, ct);
            }
            var user = new AppUser(
                Id: $"u-{Guid.NewGuid():n}"[..10],
                AadObjectId: req.AadObjectId,
                DisplayName: req.DisplayName,
                Email: req.Email,
                JobTitle: req.JobTitle,
                PersonaCodes: req.PersonaCodes,
                AddedAt: DateTime.UtcNow,
                AddedBy: addedBy);
            return await AddAsync(user, ct);
        }

        // ── Live SQL path ──────────────────────────────────────────────
        if (!Guid.TryParse(req.AadObjectId, out var oidGuid))
            throw new InvalidOperationException(
                $"AadObjectId '{req.AadObjectId}' is not a GUID. Live mode requires a real Entra objectId.");

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);

        const string UpsertUserSql = @"
            MERGE [dbo].[AppUser] AS tgt
            USING (SELECT @AadObjectId AS AadObjectId) AS src ON tgt.[AadObjectId] = src.AadObjectId
            WHEN MATCHED THEN
                UPDATE SET [DisplayName]=@DisplayName, [Email]=@Email, [Upn]=@Email,
                           [UpdatedUtc]=SYSUTCDATETIME(), [IsActive]=1
            WHEN NOT MATCHED THEN
                INSERT ([AadObjectId],[DisplayName],[Email],[Upn])
                VALUES (@AadObjectId,@DisplayName,@Email,@Email)
            OUTPUT inserted.[Id];";

        var userId = await c.ExecuteScalarAsync<int>(new CommandDefinition(UpsertUserSql,
            new { AadObjectId = oidGuid, req.DisplayName, req.Email }, tx, cancellationToken: ct));

        await ReplacePersonaRows(c, tx, userId, req.PersonaCodes, ct);
        await tx.CommitAsync(ct);

        var refreshed = await GetFromStoreAsync(userId.ToString(), ct);
        return refreshed ?? throw new InvalidOperationException($"Failed to read back user id {userId}.");
    }

    /// <summary>Replace persona list on an existing user (A5 edit dialog).</summary>
    public async Task<AppUser?> AssignPersonas(string userId, AssignPersonasRequest req, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = await GetAsync(userId, ct);
            if (existing is null) return null;
            return await Replace(existing with { PersonaCodes = req.PersonaCodes }, ct);
        }

        await using var c = await _sql.OpenAsync(ct);
        var id = await ResolveUserId(c, null, userId, ct);
        if (id is null) return null;

        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        await ReplacePersonaRows(c, tx, id.Value, req.PersonaCodes, ct);
        await tx.CommitAsync(ct);

        return await GetFromStoreAsync(id.Value.ToString(), ct);
    }

    /// <summary>Remove a user entirely.</summary>
    public async Task<bool> Delete(string userId, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = await GetAsync(userId, ct);
            if (existing is null) return false;
            SeedList.RemoveAll(u => u.Id.Equals(existing.Id, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        await using var c = await _sql.OpenAsync(ct);
        var id = await ResolveUserId(c, null, userId, ct);
        if (id is null) return false;

        // UserPersona rows cascade automatically via FK ON DELETE CASCADE.
        var rows = await c.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [dbo].[AppUser] WHERE [Id] = @id;",
            new { id = id.Value }, cancellationToken: ct));
        return rows > 0;
    }

    private async Task<AppUser> Replace(AppUser user, CancellationToken ct)
    {
        // Mocked-only helper.
        SeedList.RemoveAll(u => u.Id.Equals(user.Id, StringComparison.OrdinalIgnoreCase));
        SeedList.Add(user);
        return await Task.FromResult(user);
    }

    // ─── Store hooks ───────────────────────────────────────────────────

    protected override async Task<IReadOnlyList<AppUser>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        return await ReadUsers(c, null, whereClause: "u.[IsActive] = 1", parameters: null, ct);
    }

    protected override async Task<AppUser?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        // Accept either the internal INT Id (as string) or the AadObjectId GUID.
        string where;
        object args;
        if (int.TryParse(id, out var intId))
        {
            where = "u.[Id] = @id";
            args = new { id = intId };
        }
        else if (Guid.TryParse(id, out var g))
        {
            where = "u.[AadObjectId] = @id";
            args = new { id = g };
        }
        else return null;

        var list = await ReadUsers(c, null, where, args, ct);
        return list.SingleOrDefault();
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private static async Task<IReadOnlyList<AppUser>> ReadUsers(
        SqlConnection c, SqlTransaction? tx, string whereClause, object? parameters, CancellationToken ct)
    {
        // Two-round trip: pull AppUser rows + all UserPersona joins, then stitch.
        var userSql = $@"
            SELECT u.[Id], u.[AadObjectId], u.[DisplayName], u.[Email], u.[CreatedUtc]
            FROM   [dbo].[AppUser] u
            WHERE  {whereClause}
            ORDER BY u.[DisplayName];";

        var users = (await c.QueryAsync<(int Id, Guid AadObjectId, string DisplayName, string? Email, DateTime CreatedUtc)>(
            new CommandDefinition(userSql, parameters, tx, cancellationToken: ct))).ToList();
        if (users.Count == 0) return Array.Empty<AppUser>();

        var ids = users.Select(u => u.Id).ToArray();
        const string PersonaSql = @"
            SELECT up.[UserId], p.[Code]
            FROM   [dbo].[UserPersona] up
            JOIN   [dbo].[Persona]     p  ON p.[Id] = up.[PersonaId]
            WHERE  up.[UserId] IN @ids
            ORDER BY p.[Code];";
        var personaRows = await c.QueryAsync<(int UserId, string Code)>(
            new CommandDefinition(PersonaSql, new { ids }, tx, cancellationToken: ct));

        var personaMap = personaRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.Code).ToArray());

        return users.Select(u => new AppUser(
            Id:           u.Id.ToString(),
            AadObjectId:  u.AadObjectId.ToString(),
            DisplayName:  u.DisplayName,
            Email:        u.Email ?? string.Empty,
            JobTitle:     null,
            PersonaCodes: personaMap.TryGetValue(u.Id, out var codes) ? codes : Array.Empty<string>(),
            AddedAt:      u.CreatedUtc,
            AddedBy:      "system")).ToArray();
    }

    /// <summary>Resolve caller-supplied id (INT-as-string or AadObjectId-guid) to the INT PK.</summary>
    private static async Task<int?> ResolveUserId(SqlConnection c, SqlTransaction? tx, string id, CancellationToken ct)
    {
        if (int.TryParse(id, out var i)) return i;
        if (Guid.TryParse(id, out var g))
        {
            return await c.ExecuteScalarAsync<int?>(new CommandDefinition(
                "SELECT [Id] FROM [dbo].[AppUser] WHERE [AadObjectId] = @g;",
                new { g }, tx, cancellationToken: ct));
        }
        return null;
    }

    /// <summary>Full replace of a user's UserPersona rows to match the given code list.</summary>
    private static async Task ReplacePersonaRows(
        SqlConnection c, SqlTransaction tx, int userId, IReadOnlyList<string> personaCodes, CancellationToken ct)
    {
        await c.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [dbo].[UserPersona] WHERE [UserId] = @userId;",
            new { userId }, tx, cancellationToken: ct));

        if (personaCodes.Count == 0) return;

        const string InsertSql = @"
            INSERT INTO [dbo].[UserPersona] ([UserId],[PersonaId])
            SELECT @userId, p.[Id]
            FROM   [dbo].[Persona] p
            WHERE  p.[Code] IN @codes;";
        await c.ExecuteAsync(new CommandDefinition(InsertSql,
            new { userId, codes = personaCodes.ToArray() }, tx, cancellationToken: ct));
    }
}
