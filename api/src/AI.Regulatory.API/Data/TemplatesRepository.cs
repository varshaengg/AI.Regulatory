using System.Globalization;
using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>CTD template catalog — A2.</summary>
public sealed class TemplatesRepository : BaseRepository<CtdTemplate>
{
    private readonly ISqlConnectionFactory _sql;

    public TemplatesRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) => _sql = sql;

    protected override bool MatchesId(CtdTemplate item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<CtdTemplateModuleEntry>> ResolveForProjectAsync(string projectId, CancellationToken ct)
    {
        var all = await ListAsync(ct);
        var projectTemplates = all
            .Where(t => string.Equals(t.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var globalTemplates = all
            .Where(t => string.Equals(t.Scope, "Global", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return CtdModuleCatalog.All.Select(module =>
        {
            var template = projectTemplates.FirstOrDefault(t => SameModule(t, module.Id))
                ?? globalTemplates.FirstOrDefault(t => SameModule(t, module.Id));
            return new CtdTemplateModuleEntry(module.Id, module.Label, module.Color, template);
        }).ToArray();
    }

    public async Task<CtdTemplate> UpsertGlobalAsync(
        string moduleId,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        CancellationToken ct)
    {
        if (IsMocked)
            return UpsertSeed(null, moduleId, version, originalFileName, storagePath, uploadedBy, true);

        await using var c = await _sql.OpenAsync(ct);
        await ArchiveExistingAsync(c, null, moduleId, ct);
        var id = await InsertAsync(c, null, moduleId, version, originalFileName, storagePath, uploadedBy, ct);
        return await GetFromStoreAsync(id.ToString(), ct)
            ?? throw new InvalidOperationException($"Failed to read back CTD template {id}.");
    }

    public async Task<CtdTemplate?> UpsertProjectOverrideAsync(
        string projectId,
        string moduleId,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        CancellationToken ct)
    {
        if (IsMocked)
            return UpsertSeed(projectId, moduleId, version, originalFileName, storagePath, uploadedBy, false);

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct);
        if (projectGuid is null)
            return null;

        await using var c = await _sql.OpenAsync(ct);
        await ArchiveExistingAsync(c, projectGuid, moduleId, ct);
        var id = await InsertAsync(c, projectGuid, moduleId, version, originalFileName, storagePath, uploadedBy, ct);
        return await GetFromStoreAsync(id.ToString(), ct);
    }

    public async Task<bool> DeleteProjectOverrideAsync(string projectId, string moduleId, CancellationToken ct)
    {
        if (IsMocked)
        {
            var count = SeedList.RemoveAll(t =>
                string.Equals(t.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)
                && SameModule(t, moduleId)
                && t.Status != "Archived");
            return count > 0;
        }

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct);
        if (projectGuid is null)
            return false;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[CtdTemplate]
            SET [Status] = N'Archived'
            WHERE [ProjectId] = @ProjectId AND [ModuleId] = @ModuleId AND [Status] <> N'Archived';
            """,
            new { ProjectId = projectGuid, ModuleId = moduleId.Trim().ToUpperInvariant() },
            cancellationToken: ct));
        return rows > 0;
    }

    protected override IEnumerable<CtdTemplate> SeedData() => new[]
    {
        Make("global-m1", null, "M1", "4.2", "m1-administrative-template.pdf", "ctd-templates/global/M1/m1-administrative-template.pdf", "Sara M.", D("2025-11-10"), "Active", true),
        Make("global-m2", null, "M2", "4.2", "m2-summary-template.pdf", "ctd-templates/global/M2/m2-summary-template.pdf", "Sara M.", D("2025-11-10"), "Active", true),
        Make("global-m3", null, "M3", "4.2", "m3-quality-template.pdf", "ctd-templates/global/M3/m3-quality-template.pdf", "Sara M.", D("2025-11-10"), "Active", true),
        Make("global-m4", null, "M4", "4.2", "m4-nonclinical-template.pdf", "ctd-templates/global/M4/m4-nonclinical-template.pdf", "Sara M.", D("2025-11-10"), "Active", true),
        Make("global-m5", null, "M5", "4.2", "m5-clinical-template.pdf", "ctd-templates/global/M5/m5-clinical-template.pdf", "Sara M.", D("2025-11-10"), "Active", true),
    };

    protected override async Task<IReadOnlyList<CtdTemplate>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<CtdTemplateRow>(new CommandDefinition(
            """
            SELECT t.[Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId],
                   t.[ModuleId], t.[FileName], t.[StoragePath], t.[Version],
                   t.[UploadedBy], t.[UploadedOn], t.[Status]
            FROM [dbo].[CtdTemplate] t
            LEFT JOIN [dbo].[Project] p ON p.[Id] = t.[ProjectId]
            WHERE t.[Status] <> N'Archived'
            ORDER BY t.[ProjectId], t.[ModuleId], t.[UploadedOn] DESC;
            """,
            cancellationToken: ct));
        return rows.Select(ToTemplate).ToArray();
    }

    protected override async Task<CtdTemplate?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var templateId))
            return SeedList.FirstOrDefault(t => MatchesId(t, id));

        await using var c = await _sql.OpenAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<CtdTemplateRow>(new CommandDefinition(
            """
            SELECT t.[Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId],
                   t.[ModuleId], t.[FileName], t.[StoragePath], t.[Version],
                   t.[UploadedBy], t.[UploadedOn], t.[Status]
            FROM [dbo].[CtdTemplate] t
            LEFT JOIN [dbo].[Project] p ON p.[Id] = t.[ProjectId]
            WHERE t.[Id] = @templateId;
            """,
            new { templateId },
            cancellationToken: ct));
        return row is null ? null : ToTemplate(row);
    }

    private static bool SameModule(CtdTemplate template, string moduleId)
        => string.Equals(template.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase);

    private CtdTemplate UpsertSeed(
        string? projectId,
        string moduleId,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        bool isDefault)
    {
        var normalizedModuleId = moduleId.Trim().ToUpperInvariant();
        SeedList.RemoveAll(t =>
            string.Equals(t.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)
            && SameModule(t, normalizedModuleId)
            && t.Status != "Archived");

        var item = Make(
            Guid.NewGuid().ToString("N"),
            projectId,
            normalizedModuleId,
            version.Trim(),
            originalFileName,
            storagePath,
            uploadedBy,
            DateTime.UtcNow,
            "Active",
            isDefault);
        SeedList.Add(item);
        return item;
    }

    private static async Task ArchiveExistingAsync(SqlConnection c, Guid? projectId, string moduleId, CancellationToken ct)
    {
        var sql = projectId is null
            ? """
              UPDATE [dbo].[CtdTemplate]
              SET [Status] = N'Archived'
              WHERE [ProjectId] IS NULL AND [ModuleId] = @ModuleId AND [Status] <> N'Archived';
              """
            : """
              UPDATE [dbo].[CtdTemplate]
              SET [Status] = N'Archived'
              WHERE [ProjectId] = @ProjectId AND [ModuleId] = @ModuleId AND [Status] <> N'Archived';
              """;

        await c.ExecuteAsync(new CommandDefinition(sql, new { ProjectId = projectId, ModuleId = moduleId.Trim().ToUpperInvariant() }, cancellationToken: ct));
    }

    private static async Task<Guid> InsertAsync(
        SqlConnection c,
        Guid? projectId,
        string moduleId,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        CancellationToken ct)
    {
        return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(
            """
            INSERT INTO [dbo].[CtdTemplate] ([ProjectId], [ModuleId], [FileName], [StoragePath], [Version], [UploadedBy], [UploadedOn], [Status])
            OUTPUT INSERTED.[Id]
            VALUES (@ProjectId, @ModuleId, @FileName, @StoragePath, @Version, @UploadedBy, SYSUTCDATETIME(), N'Active');
            """,
            new
            {
                ProjectId = projectId,
                ModuleId = moduleId.Trim().ToUpperInvariant(),
                FileName = originalFileName.Trim(),
                StoragePath = storagePath.Trim(),
                Version = version.Trim(),
                UploadedBy = uploadedBy.Trim(),
            },
            cancellationToken: ct));
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

    private static CtdTemplate ToTemplate(CtdTemplateRow row)
        => Make(
            row.Id.ToString(),
            row.ProjectId,
            row.ModuleId,
            row.Version,
            row.FileName,
            row.StoragePath,
            row.UploadedBy,
            row.UploadedOn,
            row.Status,
            row.ProjectId is null);

    private static CtdTemplate Make(
        string id,
        string? projectId,
        string moduleId,
        string version,
        string fileName,
        string storagePath,
        string uploadedBy,
        DateTime uploadedOn,
        string status,
        bool isDefault)
        => new(
            Id: id,
            Country: projectId is null ? "Global" : $"Project {projectId}",
            Region: projectId is null ? "Global" : "Project override",
            Modules: new[] { moduleId.TrimStart('M') },
            Version: version,
            UploadedBy: uploadedBy,
            UploadedOn: uploadedOn,
            Status: status,
            ModuleId: moduleId,
            FileName: fileName,
            Format: "pdf",
            Scope: projectId is null ? "Global" : "Project",
            ProjectId: projectId,
            IsDefault: isDefault);

    private sealed class CtdTemplateRow
    {
        public Guid Id { get; init; }
        public string? ProjectId { get; init; }
        public string ModuleId { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string StoragePath { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string UploadedBy { get; init; } = string.Empty;
        public DateTime UploadedOn { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    private static DateTime D(string iso) => DateTime.Parse(iso).ToUniversalTime();
}
