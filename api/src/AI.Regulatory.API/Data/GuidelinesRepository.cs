using System.Collections.Concurrent;
using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Persistence for module guidelines and their checklist rules (ModuleGuideline, GuidelineRule).</summary>
public sealed class GuidelinesRepository
{
    public static readonly string[] Severities = ["Low", "Medium", "High"];

    private readonly ISqlConnectionFactory _sql;
    private readonly bool _isMocked;
    private readonly ConcurrentDictionary<string, ModuleGuideline> _mem = new();

    public GuidelinesRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
    {
        _isMocked = options.Value.IsMocked;
        _sql = sql;
    }

    public async Task<ModuleGuideline> CreateAsync(
        string moduleId, string country, string version, string fileName, string storagePath,
        string uploadedBy, string? note, IReadOnlyList<GuidelineRule> rules, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        if (_isMocked)
        {
            var g = new ModuleGuideline(id.ToString(), moduleId, country, version, fileName, uploadedBy, now, false, note, rules);
            _mem[g.Id] = g;
            return g;
        }

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO [dbo].[ModuleGuideline] ([Id],[Country],[ModuleId],[Version],[FileName],[StoragePath],[UploadedBy],[UploadedOn],[ExtractionNote])
            VALUES (@id,@country,@moduleId,@version,@fileName,@storagePath,@uploadedBy,@now,@note);
            """,
            new { id, country, moduleId, version, fileName, storagePath, uploadedBy, now, note }, tx, cancellationToken: ct));
        await InsertRulesAsync(c, tx, id, rules, ct);
        await tx.CommitAsync(ct);
        return (await GetAsync(id.ToString(), ct))!;
    }

    public async Task<IReadOnlyList<ModuleGuideline>> ListAsync(CancellationToken ct)
    {
        if (_isMocked)
            return _mem.Values.OrderByDescending(g => g.UploadedOn).ToArray();

        await using var c = await _sql.OpenAsync(ct);
        var heads = (await c.QueryAsync<Row>(new CommandDefinition(
            Select + " WHERE [Status] <> N'Archived' ORDER BY [UploadedOn] DESC;", cancellationToken: ct))).ToList();
        var rules = (await c.QueryAsync<RuleRow>(new CommandDefinition(
            "SELECT r.* FROM [dbo].[GuidelineRule] r JOIN [dbo].[ModuleGuideline] g ON g.[Id]=r.[GuidelineId] WHERE g.[Status] <> N'Archived' ORDER BY r.[SortOrder];",
            cancellationToken: ct))).ToLookup(r => r.GuidelineId);
        return heads.Select(h => ToModel(h, rules[h.Id])).ToArray();
    }

    public async Task<ModuleGuideline?> GetAsync(string id, CancellationToken ct)
    {
        if (_isMocked)
            return _mem.TryGetValue(id, out var g) ? g : null;
        if (!Guid.TryParse(id, out var guid))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        var head = await c.QuerySingleOrDefaultAsync<Row>(new CommandDefinition(
            Select + " WHERE [Id] = @guid AND [Status] <> N'Archived';", new { guid }, cancellationToken: ct));
        if (head is null) return null;
        var rules = await c.QueryAsync<RuleRow>(new CommandDefinition(
            "SELECT * FROM [dbo].[GuidelineRule] WHERE [GuidelineId] = @guid ORDER BY [SortOrder];", new { guid }, cancellationToken: ct));
        return ToModel(head, rules);
    }

    public async Task<ModuleGuideline?> SaveRulesAsync(string id, IReadOnlyList<GuidelineRule> rules, CancellationToken ct)
    {
        if (_isMocked)
        {
            if (!_mem.TryGetValue(id, out var g)) return null;
            return _mem[id] = g with { Rules = rules };
        }
        if (!Guid.TryParse(id, out var guid)) return null;

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        var exists = await c.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM [dbo].[ModuleGuideline] WHERE [Id]=@guid AND [Status] <> N'Archived';", new { guid }, tx, cancellationToken: ct));
        if (exists == 0) return null;
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM [dbo].[GuidelineRule] WHERE [GuidelineId]=@guid;", new { guid }, tx, cancellationToken: ct));
        await InsertRulesAsync(c, tx, guid, rules, ct);
        await tx.CommitAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Publishes a guideline; any other published guideline for the same country and module goes back to review.</summary>
    public async Task<ModuleGuideline?> PublishAsync(string id, CancellationToken ct)
    {
        if (_isMocked)
        {
            if (!_mem.TryGetValue(id, out var g)) return null;
            foreach (var o in _mem.Values.Where(o => o.Id != id && o.IsPublished && o.ModuleId == g.ModuleId && o.Country == g.Country))
                _mem[o.Id] = o with { IsPublished = false };
            return _mem[id] = g with { IsPublished = true };
        }
        if (!Guid.TryParse(id, out var guid)) return null;

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        var affected = await c.ExecuteAsync(new CommandDefinition(
            """
            DECLARE @country NVARCHAR(10), @module NVARCHAR(10);
            SELECT @country=[Country], @module=[ModuleId] FROM [dbo].[ModuleGuideline] WHERE [Id]=@guid AND [Status] <> N'Archived';
            IF @country IS NULL RETURN;
            UPDATE [dbo].[ModuleGuideline] SET [IsPublished]=0
              WHERE [Country]=@country AND [ModuleId]=@module AND [Id]<>@guid AND [IsPublished]=1;
            UPDATE [dbo].[ModuleGuideline] SET [IsPublished]=1 WHERE [Id]=@guid;
            """, new { guid }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return affected == 0 ? null : await GetAsync(id, ct);
    }

    public async Task<ModuleGuideline?> UnpublishAsync(string id, CancellationToken ct)
    {
        if (_isMocked)
            return _mem.TryGetValue(id, out var g) ? _mem[id] = g with { IsPublished = false } : null;
        if (!Guid.TryParse(id, out var guid)) return null;

        await using var c = await _sql.OpenAsync(ct);
        var n = await c.ExecuteAsync(new CommandDefinition(
            "UPDATE [dbo].[ModuleGuideline] SET [IsPublished]=0 WHERE [Id]=@guid AND [Status] <> N'Archived';", new { guid }, cancellationToken: ct));
        return n == 0 ? null : await GetAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct)
    {
        if (_isMocked)
            return _mem.TryRemove(id, out _);
        if (!Guid.TryParse(id, out var guid)) return false;

        await using var c = await _sql.OpenAsync(ct);
        return await c.ExecuteAsync(new CommandDefinition(
            "UPDATE [dbo].[ModuleGuideline] SET [Status]=N'Archived', [IsPublished]=0 WHERE [Id]=@guid AND [Status] <> N'Archived';",
            new { guid }, cancellationToken: ct)) > 0;
    }

    private static async Task InsertRulesAsync(System.Data.Common.DbConnection c, System.Data.Common.DbTransaction tx, Guid guid, IReadOnlyList<GuidelineRule> rules, CancellationToken ct)
    {
        var order = 0;
        foreach (var r in rules)
        {
            await c.ExecuteAsync(new CommandDefinition(
                "INSERT INTO [dbo].[GuidelineRule] ([GuidelineId],[RuleKey],[Text],[Severity],[NodeRef],[SortOrder]) VALUES (@guid,@RuleKey,@Text,@Severity,@NodeRef,@order);",
                new { guid, r.RuleKey, r.Text, r.Severity, r.NodeRef, order = order++ }, tx, cancellationToken: ct));
        }
    }

    private const string Select =
        "SELECT [Id],[Country],[ModuleId],[Version],[FileName],[UploadedBy],[UploadedOn],[IsPublished],[ExtractionNote] FROM [dbo].[ModuleGuideline]";

    private static ModuleGuideline ToModel(Row h, IEnumerable<RuleRow> rules) => new(
        h.Id.ToString(), h.ModuleId, h.Country, h.Version, h.FileName, h.UploadedBy,
        DateTime.SpecifyKind(h.UploadedOn, DateTimeKind.Utc), h.IsPublished, h.ExtractionNote,
        rules.Select(r => new GuidelineRule(r.RuleKey, r.Text, r.Severity, r.NodeRef)).ToArray());

    private sealed class Row
    {
        public Guid Id { get; set; }
        public string Country { get; set; } = "";
        public string ModuleId { get; set; } = "";
        public string Version { get; set; } = "";
        public string FileName { get; set; } = "";
        public string UploadedBy { get; set; } = "";
        public DateTime UploadedOn { get; set; }
        public bool IsPublished { get; set; }
        public string? ExtractionNote { get; set; }
    }

    private sealed class RuleRow
    {
        public Guid GuidelineId { get; set; }
        public string RuleKey { get; set; } = "";
        public string Text { get; set; } = "";
        public string Severity { get; set; } = "Medium";
        public string? NodeRef { get; set; }
    }
}
