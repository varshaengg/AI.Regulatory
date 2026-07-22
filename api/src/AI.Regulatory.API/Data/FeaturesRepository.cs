using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Feature catalog — the vertical axis of the permission matrix.</summary>
public sealed class FeaturesRepository : BaseRepository<Feature>
{
    private readonly ISqlConnectionFactory _sql;

    public FeaturesRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) { _sql = sql; }

    protected override bool MatchesId(Feature item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<Feature> SeedData() => new[]
    {
        new Feature("UserManagement",    "User Management",    "Administration", 10),
        new Feature("DossierManagement", "Dossier Management", "Regulatory",     20),
        new Feature("Templates",         "Templates",          "Regulatory",     30),
        new Feature("Assignments",       "Assignments",        "Regulatory",     40),
        new Feature("Reviews",           "Reviews",            "Regulatory",     50),
        new Feature("Notifications",     "Notifications",      "Platform",       60),
    };

    private const string SelectClause =
        "SELECT [Code], [Name], [Category], [SortOrder] FROM [dbo].[Feature] WHERE [IsActive] = 1";

    protected override async Task<IReadOnlyList<Feature>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<Feature>(new CommandDefinition(
            SelectClause + " ORDER BY [SortOrder], [Name];", cancellationToken: ct));
        return rows.ToArray();
    }

    protected override async Task<Feature?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<Feature>(new CommandDefinition(
            SelectClause + " AND [Code] = @id;", new { id }, cancellationToken: ct));
    }
}
