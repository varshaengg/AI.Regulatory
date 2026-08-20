using System.Globalization;
using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>
/// Tenant-wide default source configuration — A7 (Admin). One row per CTD
/// module (<c>UQ_GlobalSource_ModuleId</c> enforces this at the SQL layer).
/// Used as the fallback source for a module when a project has not overridden
/// it with its own <see cref="ProjectSource"/> row (see
/// <see cref="ProjectSourcesRepository.ByProjectAsync"/> for the merge).
/// </summary>
public sealed class GlobalSourcesRepository : BaseRepository<GlobalSource>
{
    private readonly ISqlConnectionFactory _sql;

    public GlobalSourcesRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) => _sql = sql;

    protected override bool MatchesId(GlobalSource item, string id) => item.Id.ToString() == id;

    /// <summary>Lookup by module (there's at most one default per module).</summary>
    public async Task<GlobalSource?> ByModuleAsync(string moduleId, CancellationToken ct)
    {
        var all = await ListAsync(ct);
        return all.FirstOrDefault(g => string.Equals(g.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Create-or-replace the default for a module (admin-only, "one default per module").</summary>
    public async Task<GlobalSource> UpsertAsync(string moduleId, UpsertGlobalSourceRequest request, ConnectionTestResult probe, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = SeedList.FirstOrDefault(g => string.Equals(g.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase));
            var nextId = existing?.Id ?? (SeedList.Select(g => g.Id).DefaultIfEmpty().Max() + 1);
            var updated = new GlobalSource(nextId, moduleId, request.Label.Trim(), request.Path.Trim(), request.Type.Trim(), probe.TestedAt, probe.Status);
            if (existing is not null) SeedList.Remove(existing);
            SeedList.Add(updated);
            return updated;
        }

        await using var c = await _sql.OpenAsync(ct);
        const string sql = """
            MERGE [dbo].[GlobalSource] AS tgt
            USING (SELECT @ModuleId AS ModuleId) AS src
                ON tgt.[ModuleId] = src.ModuleId
            WHEN MATCHED THEN
                UPDATE SET [Label] = @Label, [Path] = @Path, [Type] = @Type,
                           [SyncedAt] = @SyncedAt, [Status] = @Status
            WHEN NOT MATCHED THEN
                INSERT ([ModuleId], [Label], [Path], [Type], [SyncedAt], [Status])
                VALUES (@ModuleId, @Label, @Path, @Type, @SyncedAt, @Status)
            OUTPUT INSERTED.[Id];
            """;
        var newId = await c.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            ModuleId = moduleId,
            Label = request.Label.Trim(),
            Path = request.Path.Trim(),
            Type = request.Type.Trim(),
            SyncedAt = probe.TestedAt,
            Status = probe.Status,
        }, cancellationToken: ct));

        return await GetFromStoreAsync(newId.ToString(CultureInfo.InvariantCulture), ct)
            ?? throw new InvalidOperationException($"Failed to read back global source {newId}.");
    }

    /// <summary>Persist a re-test outcome for an existing module default.</summary>
    public async Task<GlobalSource?> SetTestResultAsync(string moduleId, ConnectionTestResult result, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = SeedList.FirstOrDefault(g => string.Equals(g.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase));
            if (existing is null) return null;
            var updated = existing with { Status = result.Status, SyncedAt = result.TestedAt };
            SeedList.Remove(existing);
            SeedList.Add(updated);
            return updated;
        }

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[GlobalSource]
            SET [Status] = @Status, [SyncedAt] = @SyncedAt
            WHERE [ModuleId] = @ModuleId;
            """,
            new { ModuleId = moduleId, Status = result.Status, SyncedAt = result.TestedAt },
            cancellationToken: ct));

        return rows == 0 ? null : await ByModuleAsync(moduleId, ct);
    }

    /// <summary>Remove the default for a module (module reverts to "no default").</summary>
    public async Task<bool> DeleteAsync(string moduleId, CancellationToken ct)
    {
        if (IsMocked)
            return SeedList.RemoveAll(g => string.Equals(g.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase)) > 0;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            "DELETE FROM [dbo].[GlobalSource] WHERE [ModuleId] = @ModuleId;",
            new { ModuleId = moduleId },
            cancellationToken: ct));
        return rows > 0;
    }

    protected override IEnumerable<GlobalSource> SeedData() => new[]
    {
        new GlobalSource(1, "M1", "Tenant default", "contosopharma/defaults/m1", "Azure Blob", D(-3, 9, 0), "ok"),
        new GlobalSource(2, "M2", "Tenant default", "contosopharma/defaults/m2", "Azure Blob", D(-3, 9, 0), "ok"),
        new GlobalSource(3, "M3", "Tenant default", "contosopharma/defaults/m3", "Azure Blob", D(-3, 9, 0), "ok"),
    };

    protected override async Task<IReadOnlyList<GlobalSource>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<GlobalSource>(new CommandDefinition(
            """
            SELECT [Id], [ModuleId], [Label], [Path], [Type], [SyncedAt], [Status]
            FROM [dbo].[GlobalSource]
            ORDER BY [ModuleId];
            """,
            cancellationToken: ct));
        return rows.ToArray();
    }

    protected override async Task<GlobalSource?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId))
            return SeedList.FirstOrDefault(g => MatchesId(g, id));

        await using var c = await _sql.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<GlobalSource>(new CommandDefinition(
            """
            SELECT [Id], [ModuleId], [Label], [Path], [Type], [SyncedAt], [Status]
            FROM [dbo].[GlobalSource]
            WHERE [Id] = @intId;
            """,
            new { intId }, cancellationToken: ct));
    }

    private static DateTime D(int daysOffset, int h, int m)
        => DateTime.UtcNow.Date.AddDays(daysOffset).AddHours(h).AddMinutes(m);
}
