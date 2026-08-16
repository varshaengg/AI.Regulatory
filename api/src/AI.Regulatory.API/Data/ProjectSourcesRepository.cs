using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Per-project source configuration — A4. Also feeds L5 module/source summary.</summary>
public sealed class ProjectSourcesRepository : BaseRepository<ProjectSource>
{
    private readonly ISqlConnectionFactory _sql;

    public ProjectSourcesRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) => _sql = sql;

    protected override bool MatchesId(ProjectSource item, string id)
        => item.Id.ToString() == id;

    /// <summary>Group all sources for a project by module for the A4 UI.</summary>
    public async Task<IReadOnlyList<ProjectSourcesByModule>> ByProjectAsync(string projectId, CancellationToken ct)
    {
        var all = await ListAsync(ct);
        var mine = all.Where(s => string.Equals(s.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList();
        return CtdModuleCatalog.All.Select(m => new ProjectSourcesByModule(
            m.Id, m.Label, m.Color,
            mine.Where(s => s.ModuleId == m.Id).ToArray())).ToArray();
    }

    protected override IEnumerable<ProjectSource> SeedData() => new[]
    {
        new ProjectSource(1, "1", "M1", "Azure Blob primary", "contosopharma/px102/m1",              "Azure Blob",  D(-1,  9,12), "ok"),
        new ProjectSource(2, "1", "M2", "Azure Blob primary", "contosopharma/px102/m2",              "Azure Blob",  D(-1,  8,45), "ok"),
        new ProjectSource(3, "1", "M3", "Drug substance data","contosopharma/px102/m3/drug-substance","Azure Blob", D(-1,  7,30), "ok"),
        new ProjectSource(4, "1", "M3", "Analytical reports", "px102-sharepoint/quality/analytical", "SharePoint",  D(-1,  7,28), "ok"),
        new ProjectSource(5, "1", "M3", "Stability studies",  "contosopharma/px102/m3/stability",    "Azure Blob",  D(-2, 22, 0), "warning"),
        new ProjectSource(6, "1", "M5", "Clinical trial data","contosopharma/px102/m5/ctr",          "Azure Blob",  D(-2, 16,40), "ok"),
        new ProjectSource(7, "1", "M5", "ISS / ISE reports",  "px102-sharepoint/clinical/iss",       "SharePoint",  D(-2, 14,10), "error"),
    };

    protected override async Task<IReadOnlyList<ProjectSource>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<ProjectSource>(new CommandDefinition(
            """
            SELECT ps.[Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId],
                   ps.[ModuleId], ps.[Label], ps.[Path], ps.[Type], ps.[SyncedAt], ps.[Status]
            FROM [dbo].[ProjectSource] ps
            JOIN [dbo].[Project] p ON p.[Id] = ps.[ProjectId]
            ORDER BY p.[ProjectNumber], ps.[ModuleId], ps.[Id];
            """,
            cancellationToken: ct));
        return rows.ToArray();
    }

    protected override async Task<ProjectSource?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId))
            return SeedList.FirstOrDefault(s => MatchesId(s, id));

        await using var c = await _sql.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<ProjectSource>(new CommandDefinition(
            """
            SELECT ps.[Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId],
                   ps.[ModuleId], ps.[Label], ps.[Path], ps.[Type], ps.[SyncedAt], ps.[Status]
            FROM [dbo].[ProjectSource] ps
            JOIN [dbo].[Project] p ON p.[Id] = ps.[ProjectId]
            WHERE ps.[Id] = @intId;
            """,
            new { intId }, cancellationToken: ct));
    }

    private static DateTime D(int daysOffset, int h, int m)
        => DateTime.UtcNow.Date.AddDays(daysOffset).AddHours(h).AddMinutes(m);
}
