using System.Text.Json;
using AI.Regulatory.API.Contracts;
using Azure.Identity;

namespace AI.Regulatory.API.Data;

public sealed record TemplateParseResult(IReadOnlyList<TemplateNode> Nodes, string? Note);

/// <summary>Proposes a section hierarchy from an uploaded template PDF. Output is a draft for admin review.</summary>
public interface ITemplateStructureParser
{
    Task<TemplateParseResult> ParseAsync(string moduleId, string fileName, Stream content, CancellationToken ct);
}

public sealed class ManualTemplateStructureParser : ITemplateStructureParser
{
    public Task<TemplateParseResult> ParseAsync(string moduleId, string fileName, Stream content, CancellationToken ct)
        => Task.FromResult(new TemplateParseResult([], "AI parsing is not configured; add sections manually."));
}

public sealed class AzureAiTemplateStructureParser : ITemplateStructureParser
{
    private const int MaxPromptChars = 60_000;
    private const int MaxNodes = 300;

    private readonly AzureAiClient _ai;
    private readonly ILogger<AzureAiTemplateStructureParser> _log;

    public AzureAiTemplateStructureParser(AzureAiClient ai, ILogger<AzureAiTemplateStructureParser> log)
    {
        _ai = ai;
        _log = log;
    }

    public async Task<TemplateParseResult> ParseAsync(string moduleId, string fileName, Stream content, CancellationToken ct)
    {
        try
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            var text = await _ai.ReadTextAsync(ms.ToArray(), ct);
            if (string.IsNullOrWhiteSpace(text))
                return new TemplateParseResult([], "No text could be read from the template; add sections manually.");

            var truncated = text.Length > MaxPromptChars;
            var nodes = await ProposeAsync(moduleId, truncated ? text[..MaxPromptChars] : text, ct);
            return new TemplateParseResult(nodes,
                truncated ? "Template was long; only the first part was analysed. Review for completeness." : null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or AuthenticationFailedException or TaskCanceledException)
        {
            _log.LogWarning(ex, "Template AI parse failed for {File}", fileName);
            return new TemplateParseResult([], "AI parsing failed; add sections manually.");
        }
    }

    private async Task<IReadOnlyList<TemplateNode>> ProposeAsync(string moduleId, string text, CancellationToken ct)
    {
        using var parsed = await _ai.ChatJsonAsync(
            "You extract the section hierarchy of a CTD (Common Technical Document) template. " +
            "Return JSON: {\"nodes\":[{\"key\":string,\"parentKey\":string|null,\"title\":string,\"guidance\":string|null,\"mandatory\":boolean}]}. " +
            "key is the section number exactly as written (for example 3.2.P.1). parentKey is the key of the enclosing section, or null for top level. " +
            "Use only sections present in the template; do not invent any. Keep guidance short. Order sections as in the document.",
            $"CTD module: {moduleId}\n\nTemplate text:\n{text}", ct);

        var nodes = new List<TemplateNode>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (parsed.RootElement.TryGetProperty("nodes", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var n in arr.EnumerateArray())
            {
                var key = Str(n, "key")?.Trim();
                var title = Str(n, "title")?.Trim();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(title) || key.Length > 50 || !seen.Add(key)) continue;
                var parent = Str(n, "parentKey")?.Trim();
                var guidance = Str(n, "guidance")?.Trim();
                var mandatory = !n.TryGetProperty("mandatory", out var m) || m.ValueKind != JsonValueKind.False;
                nodes.Add(new TemplateNode(key, string.IsNullOrEmpty(parent) ? null : parent,
                    title.Length > 300 ? title[..300] : title,
                    string.IsNullOrEmpty(guidance) ? null : guidance.Length > 2000 ? guidance[..2000] : guidance,
                    mandatory));
                if (nodes.Count == MaxNodes) break;
            }
        }

        // Drop dangling parent links so the draft is always structurally valid.
        return nodes.Select(n => n.ParentKey is not null && !seen.Contains(n.ParentKey) ? n with { ParentKey = null } : n).ToArray();
    }

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
