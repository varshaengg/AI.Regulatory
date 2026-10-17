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
        var ownTemplates = all
            .Where(t => string.Equals(t.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var projectTemplates = ownTemplates.Where(t => t.IsPublished).ToArray();
        var pendingReview = ownTemplates.Where(t => !t.IsPublished).ToArray();
        var globalDefaults = all
            .Where(t => string.Equals(t.Scope, "Global", StringComparison.OrdinalIgnoreCase) && t.IsDefault)
            .ToArray();
        var country = await GetProjectCountryAsync(projectId, ct);

        // Precedence: project override, then the project's country default, then the all-country default.
        // Within each tier a module-specific template beats a whole-CTD ("ALL") template.
        var tiers = new List<IReadOnlyList<CtdTemplate>> { projectTemplates };
        if (country is not null)
            tiers.Add(globalDefaults.Where(t => string.Equals(t.Country, country, StringComparison.OrdinalIgnoreCase)).ToArray());
        tiers.Add(globalDefaults.Where(t => string.Equals(t.Country, GlobalCountry, StringComparison.OrdinalIgnoreCase)).ToArray());

        return CtdModuleCatalog.All.Select(module =>
        {
            var template = tiers
                .Select(tier => tier.FirstOrDefault(t => SameModule(t, module.Id)) ?? tier.FirstOrDefault(t => SameModule(t, AllModules)))
                .FirstOrDefault(t => t is not null);
            var pending = pendingReview.FirstOrDefault(t => SameModule(t, module.Id));
            return new CtdTemplateModuleEntry(module.Id, module.Label, module.Color, template, pending);
        }).ToArray();
    }

    public async Task<CtdTemplate> UpsertGlobalAsync(
        string moduleId,
        string country,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        bool makeDefault,
        bool publish,
        CancellationToken ct)
    {
        country = NormalizeCountry(country);
        moduleId = moduleId.Trim().ToUpperInvariant();

        if (IsMocked)
        {
            var hasDefault = SeedList.Any(t => IsGlobalDefault(t, country, moduleId));
            var asDefault = publish && (makeDefault || !hasDefault);
            if (asDefault)
                SeedList.RemoveAll(t => IsGlobalDefault(t, country, moduleId));
            var item = Make(Guid.NewGuid().ToString("N"), null, moduleId, version.Trim(), originalFileName, storagePath, uploadedBy, DateTime.UtcNow, "Active", asDefault, country, publish);
            SeedList.Add(item);
            return item;
        }

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        var existingDefault = await c.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(1) FROM [dbo].[CtdTemplate]
            WHERE [ProjectId] IS NULL AND [Country] = @country AND [ModuleId] = @moduleId AND [IsDefault] = 1 AND [Status] <> N'Archived';
            """,
            new { country, moduleId }, tx, cancellationToken: ct));
        var isDefault = publish && (makeDefault || existingDefault == 0);
        if (isDefault)
            await ClearGlobalDefaultAsync(c, tx, country, moduleId, ct);

        var id = await c.ExecuteScalarAsync<Guid>(new CommandDefinition(
            """
            INSERT INTO [dbo].[CtdTemplate] ([ProjectId], [Country], [ModuleId], [FileName], [StoragePath], [Version], [UploadedBy], [UploadedOn], [Status], [IsDefault], [IsPublished])
            OUTPUT INSERTED.[Id]
            VALUES (NULL, @country, @moduleId, @fileName, @storagePath, @version, @uploadedBy, SYSUTCDATETIME(), N'Active', @isDefault, @publish);
            """,
            new
            {
                country,
                moduleId,
                fileName = originalFileName.Trim(),
                storagePath = storagePath.Trim(),
                version = version.Trim(),
                uploadedBy = uploadedBy.Trim(),
                isDefault,publish,
            },
            tx,
            cancellationToken: ct));
        await tx.CommitAsync(ct);
        return await GetFromStoreAsync(id.ToString(), ct)
            ?? throw new InvalidOperationException($"Failed to read back CTD template {id}.");
    }

    /// <summary>Marks a global template as the single default for its country and module.</summary>
    public async Task<CtdTemplate?> SetDefaultAsync(string id, CancellationToken ct)
    {
        if (IsMocked)
        {
            var target = SeedList.FirstOrDefault(t => MatchesId(t, id) && t.Scope == "Global" && t.Status != "Archived" && t.IsPublished);
            if (target is null) return null;
            for (var i = 0; i < SeedList.Count; i++)
            {
                var t = SeedList[i];
                if (t.Scope == "Global" && t.Status != "Archived" && t.Country == target.Country && t.ModuleId == target.ModuleId)
                    SeedList[i] = t with { IsDefault = MatchesId(t, id) };
            }
            return SeedList.First(t => MatchesId(t, id));
        }

        if (!Guid.TryParse(id, out var templateId))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        var target2 = await c.QuerySingleOrDefaultAsync<(string Country, string ModuleId)?>(new CommandDefinition(
            "SELECT [Country], [ModuleId] FROM [dbo].[CtdTemplate] WHERE [Id] = @templateId AND [ProjectId] IS NULL AND [IsPublished] = 1 AND [Status] <> N'Archived';",
            new { templateId }, tx, cancellationToken: ct));
        if (target2 is null)
            return null;

        await ClearGlobalDefaultAsync(c, tx, target2.Value.Country, target2.Value.ModuleId, ct);
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE [dbo].[CtdTemplate] SET [IsDefault] = 1 WHERE [Id] = @templateId;",
            new { templateId }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return await GetFromStoreAsync(id, ct);
    }

    /// <summary>Publishes an in-review global template; it becomes the default when its country and module has none.</summary>
    public async Task<CtdTemplate?> PublishAsync(string id, CancellationToken ct)
    {
        if (IsMocked)
        {
            var index = SeedList.FindIndex(t => MatchesId(t, id) && t.Status != "Archived");
            if (index < 0) return null;
            var current = SeedList[index];
            if (current.ProjectId is not null)
            {
                SeedList.RemoveAll(t => t.ProjectId == current.ProjectId && SameModule(t, current.ModuleId) && t.IsPublished && t.Status != "Archived");
                index = SeedList.FindIndex(t => MatchesId(t, id));
                SeedList[index] = current with { IsPublished = true };
                return SeedList[index];
            }

            var hasDefault = SeedList.Any(t => IsGlobalDefault(t, current.Country, current.ModuleId));
            SeedList[index] = current with { IsPublished = true, IsDefault = current.IsDefault || !hasDefault };
            return SeedList[index];
        }

        if (!Guid.TryParse(id, out var templateId))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE o SET [Status] = N'Archived'
            FROM [dbo].[CtdTemplate] o
            JOIN [dbo].[CtdTemplate] n ON n.[Id] = @templateId AND n.[ProjectId] = o.[ProjectId] AND n.[ModuleId] = o.[ModuleId]
            WHERE o.[Id] <> n.[Id] AND o.[IsPublished] = 1 AND o.[Status] <> N'Archived';
            """,
            new { templateId }, cancellationToken: ct));
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE t SET [IsPublished] = 1,
                   [IsDefault] = CASE WHEN t.[ProjectId] IS NOT NULL THEN 0 WHEN t.[IsDefault] = 1 OR NOT EXISTS (
                       SELECT 1 FROM [dbo].[CtdTemplate] d
                       WHERE d.[ProjectId] IS NULL AND d.[Country] = t.[Country] AND d.[ModuleId] = t.[ModuleId]
                         AND d.[IsDefault] = 1 AND d.[Status] <> N'Archived') THEN 1 ELSE 0 END
            FROM [dbo].[CtdTemplate] t
            WHERE t.[Id] = @templateId AND t.[Status] <> N'Archived';
            """,
            new { templateId }, cancellationToken: ct));
        return rows == 0 ? null : await GetFromStoreAsync(id, ct);
    }

    /// <summary>Moves a non-default template back to In review. Returns null when not found, false-state via exception-free flag.</summary>
    public async Task<(CtdTemplate? Template, bool IsDefault)> UnpublishAsync(string id, CancellationToken ct)
    {
        var current = await GetAsync(id, ct);
        if (current is null || current.Status == "Archived") return (null, false);
        if (current.IsDefault) return (current, true);

        if (IsMocked)
        {
            var index = SeedList.FindIndex(t => MatchesId(t, id));
            SeedList[index] = current with { IsPublished = false };
            return (SeedList[index], false);
        }

        await using var c = await _sql.OpenAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE [dbo].[CtdTemplate] SET [IsPublished] = 0 WHERE [Id] = @templateId AND [IsDefault] = 0;",
            new { templateId = Guid.Parse(id) }, cancellationToken: ct));
        return (await GetFromStoreAsync(id, ct), false);
    }

    public async Task<CtdTemplate?> UpsertProjectOverrideAsync(
        string projectId,
        string moduleId,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        bool publish,
        CancellationToken ct)
    {
        if (IsMocked)
            return UpsertSeed(projectId, moduleId, version, originalFileName, storagePath, uploadedBy, publish);

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct);
        if (projectGuid is null)
            return null;

        await using var c = await _sql.OpenAsync(ct);
        await ArchiveExistingAsync(c, projectGuid.Value, moduleId, publish, ct);
        var id = await InsertProjectAsync(c, projectGuid.Value, moduleId, version, originalFileName, storagePath, uploadedBy, publish, ct);
        return await GetFromStoreAsync(id.ToString(), ct);
    }
    /// <summary>Archives an active global CTD template by identifier.</summary>
    public async Task<bool> DeleteGlobalAsync(string id, CancellationToken ct)
    {
        if (IsMocked)
        {
            var removed = SeedList.FirstOrDefault(t =>
                MatchesId(t, id)
                && string.Equals(t.Scope, "Global", StringComparison.OrdinalIgnoreCase)
                && t.Status != "Archived");
            if (removed is null) return false;
            SeedList.Remove(removed);
            if (removed.IsDefault)
            {
                var next = SeedList.FindIndex(t => t.Scope == "Global" && t.IsPublished && t.Country == removed.Country && t.ModuleId == removed.ModuleId);
                if (next >= 0) SeedList[next] = SeedList[next] with { IsDefault = true };
            }

            return true;
        }

        if (!Guid.TryParse(id, out var templateId))
            return false;

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        var archived = await c.QuerySingleOrDefaultAsync<(string Country, string ModuleId, bool IsDefault)?>(new CommandDefinition(
            """
            UPDATE [dbo].[CtdTemplate]
            SET [Status] = N'Archived', [IsDefault] = 0
            OUTPUT DELETED.[Country], DELETED.[ModuleId], DELETED.[IsDefault]
            WHERE [Id] = @templateId AND [ProjectId] IS NULL AND [Status] <> N'Archived';
            """,
            new { templateId }, tx, cancellationToken: ct));
        if (archived is null)
            return false;

        if (archived.Value.IsDefault)
        {
            // Promote the most recent remaining template so the country/module keeps a default.
            await c.ExecuteAsync(new CommandDefinition(
                """
                UPDATE [dbo].[CtdTemplate] SET [IsDefault] = 1
                WHERE [Id] = (SELECT TOP 1 [Id] FROM [dbo].[CtdTemplate]
                              WHERE [ProjectId] IS NULL AND [Country] = @Country AND [ModuleId] = @ModuleId AND [IsPublished] = 1 AND [Status] <> N'Archived'
                              ORDER BY [UploadedOn] DESC);
                """,
                new { archived.Value.Country, archived.Value.ModuleId }, tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return true;
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

    internal const string GlobalCountry = "Global";
    internal const string AllModules = "ALL";

    /// <summary>Normalizes a country to "Global" or an upper-case ISO 3166 alpha-2 code.</summary>
    internal static string NormalizeCountry(string? country)
        => string.IsNullOrWhiteSpace(country) || string.Equals(country.Trim(), GlobalCountry, StringComparison.OrdinalIgnoreCase)
            ? GlobalCountry
            : country.Trim().ToUpperInvariant();

    private static bool IsGlobalDefault(CtdTemplate t, string country, string moduleId)
        => t.Scope == "Global" && t.Status != "Archived" && t.IsDefault
           && string.Equals(t.Country, country, StringComparison.OrdinalIgnoreCase)
           && string.Equals(t.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase);

    private static Task ClearGlobalDefaultAsync(SqlConnection c, SqlTransaction tx, string country, string moduleId, CancellationToken ct)
        => c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[CtdTemplate] SET [IsDefault] = 0
            WHERE [ProjectId] IS NULL AND [Country] = @country AND [ModuleId] = @moduleId AND [IsDefault] = 1;
            """,
            new { country, moduleId }, tx, cancellationToken: ct));

    private async Task<string?> GetProjectCountryAsync(string projectId, CancellationToken ct)
    {
        if (IsMocked || !int.TryParse(projectId, NumberStyles.None, CultureInfo.InvariantCulture, out var projectNumber))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        return await c.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT [Country] FROM [dbo].[Project] WHERE [ProjectNumber] = @projectNumber;",
            new { projectNumber }, cancellationToken: ct));
    }

    protected override async Task<IReadOnlyList<CtdTemplate>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<CtdTemplateRow>(new CommandDefinition(
            """
            SELECT t.[Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId], t.[Country], t.[IsDefault], t.[IsPublished],
                   t.[ModuleId], t.[FileName], t.[StoragePath], t.[Version],
                   t.[UploadedBy], t.[UploadedOn], t.[Status]
            FROM [dbo].[CtdTemplate] t
            LEFT JOIN [dbo].[Project] p ON p.[Id] = t.[ProjectId]
            WHERE t.[Status] <> N'Archived'
            ORDER BY t.[ProjectId], t.[Country], t.[ModuleId], t.[IsDefault] DESC, t.[UploadedOn] DESC;
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
            SELECT t.[Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId], t.[Country], t.[IsDefault], t.[IsPublished],
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

    /// <summary>Relative storage path of the template PDF; null in mocked mode or when the template is unknown.</summary>
    public async Task<string?> GetStoragePathAsync(string id, CancellationToken ct)
    {
        if (IsMocked || !Guid.TryParse(id, out var templateId))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT [StoragePath] FROM [dbo].[CtdTemplate] WHERE [Id] = @templateId AND [Status] <> N'Archived';",
            new { templateId }, cancellationToken: ct));
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
        bool publish)
    {
        var normalizedModuleId = moduleId.Trim().ToUpperInvariant();
        SeedList.RemoveAll(t =>
            string.Equals(t.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)
            && SameModule(t, normalizedModuleId)
            && t.Status != "Archived"
            && (publish || !t.IsPublished));

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
            false,
            isPublished: publish);
        SeedList.Add(item);
        return item;
    }

    private static async Task ArchiveExistingAsync(SqlConnection c, Guid projectId, string moduleId, bool publish, CancellationToken ct)
        => await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[CtdTemplate]
            SET [Status] = N'Archived'
            WHERE [ProjectId] = @ProjectId AND [ModuleId] = @ModuleId AND [Status] <> N'Archived'
              AND (@Publish = 1 OR [IsPublished] = 0);
            """,
            new { ProjectId = projectId, ModuleId = moduleId.Trim().ToUpperInvariant(), Publish = publish },
            cancellationToken: ct));

    private static async Task<Guid> InsertProjectAsync(
        SqlConnection c,
        Guid projectId,
        string moduleId,
        string version,
        string originalFileName,
        string storagePath,
        string uploadedBy,
        bool publish,
        CancellationToken ct)
    {
        return await c.ExecuteScalarAsync<Guid>(new CommandDefinition(
            """
            INSERT INTO [dbo].[CtdTemplate] ([ProjectId], [Country], [ModuleId], [FileName], [StoragePath], [Version], [UploadedBy], [UploadedOn], [Status], [IsDefault], [IsPublished])
            OUTPUT INSERTED.[Id]
            VALUES (@ProjectId, N'Global', @ModuleId, @FileName, @StoragePath, @Version, @UploadedBy, SYSUTCDATETIME(), N'Active', 0, @Publish);
            """,
            new
            {
                ProjectId = projectId,
                ModuleId = moduleId.Trim().ToUpperInvariant(),
                FileName = originalFileName.Trim(),
                StoragePath = storagePath.Trim(),
                Version = version.Trim(),
                UploadedBy = uploadedBy.Trim(),
                Publish = publish,
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
            row.ProjectId is null && row.IsDefault,
            row.Country,
            row.IsPublished);

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
        bool isDefault,
        string country = GlobalCountry,
        bool isPublished = true)
        => new(
            Id: id,
            Country: projectId is null ? country : $"Project {projectId}",
            Region: projectId is null ? (country == GlobalCountry ? "Global" : "Country") : "Project override",
            Modules: moduleId == AllModules ? new[] { "1", "2", "3", "4", "5" } : new[] { moduleId.TrimStart('M') },
            Version: version,
            UploadedBy: uploadedBy,
            UploadedOn: uploadedOn,
            Status: status,
            ModuleId: moduleId,
            FileName: fileName,
            Format: "pdf",
            Scope: projectId is null ? "Global" : "Project",
            ProjectId: projectId,
            IsDefault: isDefault,
            IsPublished: isPublished);

    private sealed class CtdTemplateRow
    {
        public Guid Id { get; init; }
        public string? ProjectId { get; init; }
        public string Country { get; init; } = GlobalCountry;
        public bool IsDefault { get; init; }
        public bool IsPublished { get; init; } = true;
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
