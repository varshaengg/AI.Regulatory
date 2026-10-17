using System.Collections.Concurrent;
using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Persistence for a template's parsed hierarchy (CtdTemplateStructure, CtdTemplateNode).</summary>
public sealed class TemplateStructureRepository
{
    private readonly ISqlConnectionFactory _sql;
    private readonly bool _isMocked;
    private readonly ConcurrentDictionary<string, TemplateStructure> _mem = new();

    public TemplateStructureRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
    {
        _isMocked = options.Value.IsMocked;
        _sql = sql;
    }

    public async Task<TemplateStructure?> GetAsync(string templateId, CancellationToken ct)
    {
        if (_isMocked)
            return _mem.TryGetValue(templateId, out var s) ? s : null;
        if (!Guid.TryParse(templateId, out var guid))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        var head = await c.QuerySingleOrDefaultAsync<HeadRow>(new CommandDefinition(
            "SELECT [Status],[Note],[UpdatedBy],[UpdatedOn] FROM [dbo].[CtdTemplateStructure] WHERE [TemplateId]=@guid;",
            new { guid }, cancellationToken: ct));
        if (head is null) return null;
        var nodes = await c.QueryAsync<NodeRow>(new CommandDefinition(
            "SELECT [NodeKey],[ParentKey],[Title],[Guidance],[IsMandatory] FROM [dbo].[CtdTemplateNode] WHERE [TemplateId]=@guid ORDER BY [SortOrder];",
            new { guid }, cancellationToken: ct));
        return new TemplateStructure(templateId, head.Status, head.Note, head.UpdatedBy,
            DateTime.SpecifyKind(head.UpdatedOn, DateTimeKind.Utc),
            nodes.Select(n => new TemplateNode(n.NodeKey, n.ParentKey, n.Title, n.Guidance, n.IsMandatory)).ToArray());
    }

    /// <summary>Replaces the whole hierarchy; the caller has already validated it.</summary>
    public async Task<TemplateStructure> SaveAsync(
        string templateId, string status, string? note, string updatedBy, IReadOnlyList<TemplateNode> nodes, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (_isMocked)
            return _mem[templateId] = new TemplateStructure(templateId, status, note, updatedBy, now, nodes);

        var guid = Guid.Parse(templateId);
        await using var c = await _sql.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            """
            MERGE [dbo].[CtdTemplateStructure] AS t
            USING (SELECT @guid AS TemplateId) AS s ON t.[TemplateId]=s.TemplateId
            WHEN MATCHED THEN UPDATE SET [Status]=@status,[Note]=@note,[UpdatedBy]=@updatedBy,[UpdatedOn]=@now
            WHEN NOT MATCHED THEN INSERT ([TemplateId],[Status],[Note],[UpdatedBy],[UpdatedOn]) VALUES (@guid,@status,@note,@updatedBy,@now);
            DELETE FROM [dbo].[CtdTemplateNode] WHERE [TemplateId]=@guid;
            """,
            new { guid, status, note, updatedBy, now }, tx, cancellationToken: ct));

        var order = 0;
        foreach (var n in nodes)
        {
            await c.ExecuteAsync(new CommandDefinition(
                "INSERT INTO [dbo].[CtdTemplateNode] ([TemplateId],[NodeKey],[ParentKey],[Title],[Guidance],[IsMandatory],[SortOrder]) VALUES (@guid,@NodeKey,@ParentKey,@Title,@Guidance,@IsMandatory,@order);",
                new { guid, n.NodeKey, n.ParentKey, n.Title, n.Guidance, n.IsMandatory, order = order++ }, tx, cancellationToken: ct));
        }
        await tx.CommitAsync(ct);
        return new TemplateStructure(templateId, status, note, updatedBy, now, nodes);
    }

    private sealed class HeadRow
    {
        public string Status { get; set; } = "Draft";
        public string? Note { get; set; }
        public string UpdatedBy { get; set; } = "";
        public DateTime UpdatedOn { get; set; }
    }

    private sealed class NodeRow
    {
        public string NodeKey { get; set; } = "";
        public string? ParentKey { get; set; }
        public string Title { get; set; } = "";
        public string? Guidance { get; set; }
        public bool IsMandatory { get; set; }
    }
}
