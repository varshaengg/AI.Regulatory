using System.Globalization;
using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Per-project source configuration — A4. Also feeds L5 module/source summary.</summary>
public sealed class ProjectSourcesRepository : BaseRepository<ProjectSource>
{
    private readonly ISqlConnectionFactory _sql;
    private readonly GlobalSourcesRepository _globalSources;

    public ProjectSourcesRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql, GlobalSourcesRepository globalSources)
        : base(options)
    {
        _sql = sql;
        _globalSources = globalSources;
    }

    protected override bool MatchesId(ProjectSource item, string id)
        => item.Id.ToString() == id;

    /// <summary>
    /// Group all sources for a project by module for the A4 UI. A module with no
    /// project-specific source falls back to the tenant-wide <see cref="GlobalSource"/>
    /// default (flagged <c>IsDefault = true</c>, <c>Id = 0</c> so the UI knows to offer
    /// "Override" instead of Edit/Test/Remove — replace semantics: once a project has
    /// its own row for a module, the global default is no longer shown for it).
    /// </summary>
    public async Task<IReadOnlyList<ProjectSourcesByModule>> ByProjectAsync(string projectId, CancellationToken ct)
    {
        var all = await ListAsync(ct);
        var mine = all.Where(s => string.Equals(s.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList();
        var defaults = await _globalSources.ListAsync(ct);

        return CtdModuleCatalog.All.Select(m =>
        {
            var overrides = mine.Where(s => s.ModuleId == m.Id).ToArray();
            if (overrides.Length > 0)
                return new ProjectSourcesByModule(m.Id, m.Label, m.Color, overrides);

            var fallback = defaults.FirstOrDefault(g => string.Equals(g.ModuleId, m.Id, StringComparison.OrdinalIgnoreCase));
            var synthesized = fallback is null
                ? Array.Empty<ProjectSource>()
                : new[]
                {
                    new ProjectSource(0, projectId, fallback.ModuleId, fallback.Label, fallback.Path,
                        fallback.Type, fallback.SyncedAt, fallback.Status, IsDefault: true),
                };
            return new ProjectSourcesByModule(m.Id, m.Label, m.Color, synthesized);
        }).ToArray();
    }

    public override Task<ProjectSource> AddAsync(ProjectSource item, CancellationToken ct = default)
    {
        if (!IsMocked)
            return base.AddAsync(item, ct);

        var nextId = SeedList.Select(s => s.Id).DefaultIfEmpty().Max() + 1;
        return base.AddAsync(item with { Id = nextId }, ct);
    }

    /// <summary>Edit label/path/type for an existing source (scoped to its project).</summary>
    public async Task<ProjectSource?> UpdateAsync(string projectId, string id, UpdateProjectSourceRequest request, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = SeedList.FirstOrDefault(s => MatchesId(s, id) && SameProject(s, projectId));
            if (existing is null) return null;

            var updated = existing with
            {
                Label = request.Label.Trim(),
                Path = request.Path.Trim(),
                Type = request.Type.Trim(),
            };
            SeedList.Remove(existing);
            SeedList.Add(updated);
            return updated;
        }

        if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var intId))
            return null;

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct);
        if (projectGuid is null) return null;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[ProjectSource]
            SET [Label] = @Label, [Path] = @Path, [Type] = @Type
            WHERE [Id] = @Id AND [ProjectId] = @ProjectGuid;
            """,
            new
            {
                Id = intId,
                Label = request.Label.Trim(),
                Path = request.Path.Trim(),
                Type = request.Type.Trim(),
                ProjectGuid = projectGuid,
            },
            cancellationToken: ct));

        return rows == 0 ? null : await GetFromStoreAsync(id, ct);
    }

    /// <summary>Persist the outcome of a connectivity probe (status + last-synced timestamp).</summary>
    public async Task<ProjectSource?> SetTestResultAsync(string projectId, string id, ConnectionTestResult result, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = SeedList.FirstOrDefault(s => MatchesId(s, id) && SameProject(s, projectId));
            if (existing is null) return null;

            var updated = existing with { Status = result.Status, SyncedAt = result.TestedAt };
            SeedList.Remove(existing);
            SeedList.Add(updated);
            return updated;
        }

        if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var intId))
            return null;

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct);
        if (projectGuid is null) return null;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[ProjectSource]
            SET [Status] = @Status, [SyncedAt] = @SyncedAt
            WHERE [Id] = @Id AND [ProjectId] = @ProjectGuid;
            """,
            new { Id = intId, Status = result.Status, SyncedAt = result.TestedAt, ProjectGuid = projectGuid },
            cancellationToken: ct));

        return rows == 0 ? null : await GetFromStoreAsync(id, ct);
    }

    /// <summary>Remove a source (scoped to its project so callers can't cross project boundaries).</summary>
    public async Task<bool> DeleteAsync(string projectId, string id, CancellationToken ct)
    {
        if (IsMocked)
            return SeedList.RemoveAll(s => MatchesId(s, id) && SameProject(s, projectId)) > 0;

        if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var intId))
            return false;

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct);
        if (projectGuid is null) return false;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [dbo].[ProjectSource] WHERE [Id] = @Id AND [ProjectId] = @ProjectGuid;",
            new { Id = intId, ProjectGuid = projectGuid },
            cancellationToken: ct));
        return rows > 0;
    }

    private static bool SameProject(ProjectSource item, string projectId)
        => string.Equals(item.ProjectId, projectId, StringComparison.OrdinalIgnoreCase);

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
                   ps.[ModuleId], ps.[Label], ps.[Path], ps.[Type], ps.[SyncedAt], ps.[Status],
                   CAST(0 AS BIT) AS [IsDefault]
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
                   ps.[ModuleId], ps.[Label], ps.[Path], ps.[Type], ps.[SyncedAt], ps.[Status],
                   CAST(0 AS BIT) AS [IsDefault]
            FROM [dbo].[ProjectSource] ps
            JOIN [dbo].[Project] p ON p.[Id] = ps.[ProjectId]
            WHERE ps.[Id] = @intId;
            """,
            new { intId }, cancellationToken: ct));
    }

    protected override async Task<ProjectSource> AddToStoreAsync(ProjectSource item, CancellationToken ct)
    {
        var projectGuid = await ResolveProjectGuidAsync(item.ProjectId, ct)
            ?? throw new InvalidOperationException($"Project '{item.ProjectId}' not found.");

        await using var c = await _sql.OpenAsync(ct);
        const string insertSql = """
            INSERT INTO [dbo].[ProjectSource] ([ProjectId], [ModuleId], [Label], [Path], [Type], [SyncedAt], [Status])
            OUTPUT INSERTED.[Id]
            VALUES (@ProjectId, @ModuleId, @Label, @Path, @Type, @SyncedAt, @Status);
            """;

        var newId = await c.ExecuteScalarAsync<int>(new CommandDefinition(insertSql, new
        {
            ProjectId = projectGuid,
            item.ModuleId,
            item.Label,
            item.Path,
            item.Type,
            item.SyncedAt,
            item.Status,
        }, cancellationToken: ct));

        return await GetFromStoreAsync(newId.ToString(CultureInfo.InvariantCulture), ct)
            ?? throw new InvalidOperationException($"Failed to read back source {newId}.");
    }

    private async Task<Guid?> ResolveProjectGuidAsync(string projectId, CancellationToken ct)
    {
        if (!int.TryParse(projectId, NumberStyles.None, CultureInfo.InvariantCulture, out var projectNumber))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        return await c.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT [Id] FROM [dbo].[Project] WHERE [ProjectNumber] = @projectNumber;",
            new { projectNumber }, cancellationToken: ct));
    }

    private static DateTime D(int daysOffset, int h, int m)
        => DateTime.UtcNow.Date.AddDays(daysOffset).AddHours(h).AddMinutes(m);
}
