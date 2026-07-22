using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Permission verbs — the "Z" axis of a persona × feature cell.</summary>
/// <remarks>
/// Verbs are ordered Read &lt; Write &lt; Review &lt; Admin. Admin implies all
/// lower verbs when the effective permission is computed — see
/// <see cref="PermissionMatrixRepository.GetMatrix"/>.
/// </remarks>
public sealed class PermissionsRepository : BaseRepository<Permission>
{
    private readonly ISqlConnectionFactory _sql;

    public PermissionsRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) { _sql = sql; }

    protected override bool MatchesId(Permission item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<Permission> SeedData() => new[]
    {
        new Permission("Read",   "Read",   10),
        new Permission("Write",  "Write",  20),
        new Permission("Review", "Review", 30),
        new Permission("Admin",  "Admin",  40),
    };

    protected override async Task<IReadOnlyList<Permission>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<Permission>(new CommandDefinition(
            "SELECT [Code], [Name], [SortOrder] FROM [dbo].[Permission] ORDER BY [SortOrder];",
            cancellationToken: ct));
        return rows.ToArray();
    }

    protected override async Task<Permission?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<Permission>(new CommandDefinition(
            "SELECT [Code], [Name], [SortOrder] FROM [dbo].[Permission] WHERE [Code] = @id;",
            new { id }, cancellationToken: ct));
    }
}
