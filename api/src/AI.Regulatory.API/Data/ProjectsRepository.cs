using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AI.Regulatory.API.Data;

/// <summary>Projects — L1 tile + L2 catalog.</summary>
public sealed class ProjectsRepository : BaseRepository<ProjectDetail>
{
    private readonly ISqlConnectionFactory _sql;

    public ProjectsRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
        : base(options) => _sql = sql;

    protected override bool MatchesId(ProjectDetail item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<ProjectDetail> SeedData() => new[]
    {
        Make("px-102-de", "PX-102 · DE · Initial",  "DE", "In progress", "PX-102", new[] {"M1","M2","M3","M4","M5"}, "Marcus L.", 55, -42, -1),
        Make("px-102-fr", "PX-102 · FR · Initial",  "FR", "Reviewing",   "PX-102", new[] {"M1","M2","M3"},           "Marcus L.", 82, -30, -6),
        Make("el-201-it", "EL-201 · IT · Renewal",  "IT", "Blocked",     "EL-201", new[] {"M1","M2","M3","M5"},      "Aisha K.",  35, -28, -4),
        Make("el-201-es", "EL-201 · ES · Variation","ES", "Draft",       "EL-201", new[] {"M2","M3"},                "Marcus L.", 18,  -8, -2),
        Make("px-102-nl", "PX-102 · NL · Variation","NL", "Submitted",   "PX-102", new[] {"M1","M3","M5"},           "Tom K.",   100, -70,-40),
        Make("cv-304-de", "CV-304 · DE · Initial",  "DE", "In progress", "CV-304", new[] {"M1","M2","M3","M4","M5"}, "Aisha K.",  72,  -6, -1),
        Make("cv-304-fr", "CV-304 · FR · Initial",  "FR", "Reviewing",   "CV-304", new[] {"M1","M2","M3"},           "Tom K.",    40,  -5, -1),
        Make("px-102-uk", "PX-102 · UK · Initial",  "UK", "Draft",       "PX-102", new[] {"M1","M2","M3","M4","M5"}, "Marcus L.",  0,  -2, -1),
    };

    protected override async Task<IReadOnlyList<ProjectDetail>> ListFromStoreAsync(CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var projects = (await c.QueryAsync<ProjectRow>(new CommandDefinition(
            """
            SELECT [Id], [Name], [Country], [Status], [Product], [ProductVersion], [Procedure], [TargetSubmissionDate], [Applicant],
                   [Description], [DiscoveryStarted], [CtdTemplateVersionId],
                   [OwnerEmail], [OwnerDisplayName], [ProgressPct],
                   [CreatedUtc], [UpdatedUtc], [CreatedBy]
            FROM [dbo].[Project]
            ORDER BY [UpdatedUtc] DESC, [CreatedUtc] DESC;
            """,
            cancellationToken: ct))).ToList();

        if (projects.Count == 0)
            return Array.Empty<ProjectDetail>();

        var modules = await LoadModulesByProjectAsync(c, projects.Select(p => p.Id).ToArray(), ct);
        return projects.Select(p => ToDetail(p, modules.TryGetValue(p.Id, out var moduleIds) ? moduleIds : Array.Empty<string>())).ToArray();
    }

    protected override async Task<ProjectDetail?> GetFromStoreAsync(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
            return SeedList.FirstOrDefault(p => MatchesId(p, id));

        await using var c = await _sql.OpenAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<ProjectRow>(new CommandDefinition(
            """
            SELECT [Id], [Name], [Country], [Status], [Product], [ProductVersion], [Procedure], [TargetSubmissionDate], [Applicant],
                   [Description], [DiscoveryStarted], [CtdTemplateVersionId],
                   [OwnerEmail], [OwnerDisplayName], [ProgressPct],
                   [CreatedUtc], [UpdatedUtc], [CreatedBy]
            FROM [dbo].[Project]
            WHERE [Id] = @guid;
            """,
            new { guid }, cancellationToken: ct));

        if (row is null)
            return null;

        var modules = await LoadModulesByProjectAsync(c, new[] { row.Id }, ct);
        return ToDetail(row, modules.TryGetValue(row.Id, out var moduleIds) ? moduleIds : Array.Empty<string>());
    }

    protected override async Task<ProjectDetail> AddToStoreAsync(ProjectDetail item, CancellationToken ct)
    {
        await using var c = await _sql.OpenAsync(ct);
        var guid = Guid.TryParse(item.Id, out var parsed) ? parsed : Guid.NewGuid();
        var now = item.CreatedAt;

        const string insertSql = """
            INSERT INTO [dbo].[Project]
                ([Id], [TenantId], [Name], [Status], [Country], [Product], [ProductVersion], [Procedure], [TargetSubmissionDate], [Applicant],
                 [Description], [DiscoveryStarted], [CtdTemplateVersionId], [OwnerEmail], [OwnerDisplayName],
                 [ProgressPct], [CreatedUtc], [UpdatedUtc], [CreatedBy])
            VALUES
                (@Id, @TenantId, @Name, @Status, @Country, @Product, @ProductVersion, @Procedure, @TargetSubmissionDate, @Applicant,
                 @Description, @DiscoveryStarted, @CtdTemplateVersionId, @OwnerEmail, @OwnerDisplayName,
                 @ProgressPct, @CreatedUtc, @UpdatedUtc, @CreatedBy);
            """;

        await c.ExecuteAsync(new CommandDefinition(insertSql, new
        {
            Id = guid,
            TenantId = Guid.Empty,
            Name = item.Name,
            Status = ToStatusCode(item.Status),
            Country = item.Country,
            Product = item.Product,
            ProductVersion = item.ProductVersion,
            Procedure = item.Procedure,
            TargetSubmissionDate = item.TargetSubmissionDate?.ToDateTime(TimeOnly.MinValue),
            Applicant = item.OwnerDisplayName,
            Description = (string?)null,
            DiscoveryStarted = false,
            CtdTemplateVersionId = (Guid?)null,
            OwnerEmail = string.IsNullOrWhiteSpace(item.OwnerEmail) ? "unknown@example.com" : item.OwnerEmail,
            OwnerDisplayName = item.OwnerDisplayName,
            ProgressPct = item.ProgressPct,
            CreatedUtc = now,
            UpdatedUtc = item.UpdatedAt,
            CreatedBy = item.OwnerEmail
        }, cancellationToken: ct));

        return await GetFromStoreAsync(guid.ToString(), ct) ?? item with { Id = guid.ToString() };
    }

    public async Task<ProjectDetail?> UpdateAsync(
        string id,
        UpdateProjectRequest request,
        string etag,
        CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = await GetAsync(id, ct);
            if (existing is null || !string.Equals(existing.Etag, etag, StringComparison.Ordinal))
                return null;

            var updated = existing with
            {
                Name = request.Name,
                Country = request.Country,
                Product = request.Product?.Trim() ?? string.Empty,
                ProductVersion = request.ProductVersion?.Trim() ?? string.Empty,
                Procedure = request.Procedure?.Trim() ?? "Initial",
                TargetSubmissionDate = request.TargetSubmissionDate,
                OwnerDisplayName = request.OwnerDisplayName?.Trim() ?? existing.OwnerDisplayName,
                UpdatedAt = DateTime.UtcNow,
            };
            updated = updated with { Etag = ToEtag(updated.UpdatedAt) };
            SeedList.RemoveAll(project => MatchesId(project, id));
            SeedList.Add(updated);
            return updated;
        }

        if (!Guid.TryParse(id, out var projectId) || !TryParseEtag(etag, out var updatedUtc))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[Project]
            SET [Name] = @Name,
                [Country] = @Country,
                [Product] = @Product,
                [ProductVersion] = @ProductVersion,
                [Procedure] = @Procedure,
                [TargetSubmissionDate] = @TargetSubmissionDate,
                [Applicant] = @OwnerDisplayName,
                [OwnerDisplayName] = @OwnerDisplayName,
                [UpdatedUtc] = SYSUTCDATETIME()
            WHERE [Id] = @ProjectId
              AND [UpdatedUtc] = @UpdatedUtc;
            """,
            new
            {
                ProjectId = projectId,
                request.Name,
                request.Country,
                Product = request.Product?.Trim() ?? string.Empty,
                ProductVersion = request.ProductVersion?.Trim() ?? string.Empty,
                Procedure = request.Procedure?.Trim() ?? "Initial",
                TargetSubmissionDate = request.TargetSubmissionDate?.ToDateTime(TimeOnly.MinValue),
                OwnerDisplayName = request.OwnerDisplayName?.Trim() ?? string.Empty,
                UpdatedUtc = updatedUtc,
            },
            cancellationToken: ct));

        return rows == 0 ? null : await GetFromStoreAsync(id, ct);
    }

    public async Task<bool> ArchiveAsync(string id, CancellationToken ct)
    {
        if (IsMocked)
        {
            var existing = await GetAsync(id, ct);
            if (existing is null)
                return false;

            var archived = existing with { Status = "Archived", UpdatedAt = DateTime.UtcNow };
            archived = archived with { Etag = ToEtag(archived.UpdatedAt) };
            SeedList.RemoveAll(project => MatchesId(project, id));
            SeedList.Add(archived);
            return true;
        }

        if (!Guid.TryParse(id, out var projectId))
            return false;

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.ExecuteAsync(new CommandDefinition(
            """
            UPDATE [dbo].[Project]
            SET [Status] = 2,
                [UpdatedUtc] = SYSUTCDATETIME()
            WHERE [Id] = @ProjectId
              AND [Status] <> 2;
            """,
            new { ProjectId = projectId },
            cancellationToken: ct));
        return rows > 0;
    }

    private static ProjectDetail ToDetail(ProjectRow row, IReadOnlyList<string> modules)
        => new(
            Id: row.Id.ToString(),
            Name: row.Name,
            Country: row.Country,
            Status: FromStatusCode(row.Status),
            Product: row.Product,
            ProductVersion: row.ProductVersion,
            Procedure: row.Procedure,
            TargetSubmissionDate: row.TargetSubmissionDate is { } targetSubmissionDate
                ? DateOnly.FromDateTime(targetSubmissionDate)
                : null,
            Modules: modules,
            OwnerEmail: row.OwnerEmail,
            OwnerDisplayName: row.OwnerDisplayName,
            ProgressPct: row.ProgressPct,
            CreatedAt: row.CreatedUtc,
            UpdatedAt: row.UpdatedUtc,
            Etag: ToEtag(row.UpdatedUtc));

    private static string ToEtag(DateTime updatedUtc) => $"\"{updatedUtc.Ticks:x}\"";

    private static bool TryParseEtag(string etag, out DateTime updatedUtc)
    {
        var token = etag.Trim().Trim('"');
        if (long.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var ticks))
        {
            updatedUtc = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }

        updatedUtc = default;
        return false;
    }

    private static byte ToStatusCode(string status)
    {
        var s = status.Trim().ToLowerInvariant();
        return s switch
        {
            "draft" => 0,
            "active" or "in progress" or "reviewing" or "submitted" => 1,
            "archived" => 2,
            _ => 0,
        };
    }

    private static string FromStatusCode(byte status)
        => status switch
        {
            0 => "Draft",
            1 => "Active",
            2 => "Archived",
            _ => "Draft",
        };

    private static async Task<Dictionary<Guid, IReadOnlyList<string>>> LoadModulesByProjectAsync(
        SqlConnection c, Guid[] projectIds, CancellationToken ct)
    {
        if (projectIds.Length == 0)
            return new();

        const string sql = """
            SELECT DISTINCT [ProjectId], [ModuleId]
            FROM [dbo].[ProjectSource]
            WHERE [ProjectId] IN @projectIds
            ORDER BY [ProjectId], [ModuleId];
            """;

        var rows = await c.QueryAsync<ProjectModuleRow>(new CommandDefinition(sql, new { projectIds }, cancellationToken: ct));
        return rows
            .GroupBy(r => r.ProjectId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.ModuleId).Distinct().ToArray());
    }

    private sealed class ProjectRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public byte Status { get; init; }
        public string Product { get; init; } = string.Empty;
        public string ProductVersion { get; init; } = string.Empty;
        public string Procedure { get; init; } = string.Empty;
        public DateTime? TargetSubmissionDate { get; init; }
        public string Applicant { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool DiscoveryStarted { get; init; }
        public Guid? CtdTemplateVersionId { get; init; }
        public string OwnerEmail { get; init; } = string.Empty;
        public string OwnerDisplayName { get; init; } = string.Empty;
        public int ProgressPct { get; init; }
        public DateTime CreatedUtc { get; init; }
        public DateTime UpdatedUtc { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
    }

    private sealed class ProjectModuleRow
    {
        public Guid ProjectId { get; init; }
        public string ModuleId { get; init; } = string.Empty;
    }

    private static ProjectDetail Make(string id, string name, string country, string status, string product,
        string[] modules, string owner, int pct, int createdDaysAgo, int updatedDaysAgo)
        => new(id, name, country, status, product, string.Empty, "Initial", null, modules,
               $"{owner.Replace(" ", "").ToLower()}@ucatalyst.onmicrosoft.com",
               owner, pct,
               DateTime.UtcNow.AddDays(createdDaysAgo), DateTime.UtcNow.AddDays(updatedDaysAgo),
               "\"" + Guid.NewGuid().ToString("N")[..8] + "\"");
}
