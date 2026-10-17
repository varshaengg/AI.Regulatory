using System.Text.Json;
using AI.Regulatory.API.Contracts;
using Azure.Identity;

namespace AI.Regulatory.API.Data;

public sealed record GuidelineExtraction(IReadOnlyList<GuidelineRule> Rules, string? Note);

/// <summary>Turns an uploaded guideline document into draft checklist rules for admin review.</summary>
public interface IGuidelineRuleExtractor
{
    Task<GuidelineExtraction> ExtractAsync(string moduleId, string fileName, Stream content, CancellationToken ct);
}

/// <summary>Used when Document Intelligence / Azure OpenAI are not configured: admin adds rules by hand.</summary>
public sealed class ManualGuidelineRuleExtractor : IGuidelineRuleExtractor
{
    public Task<GuidelineExtraction> ExtractAsync(string moduleId, string fileName, Stream content, CancellationToken ct)
        => Task.FromResult(new GuidelineExtraction([], "AI extraction is not configured; add rules manually."));
}

/// <summary>Document Intelligence for text, then Azure OpenAI to propose rules.</summary>
public sealed class AzureAiGuidelineRuleExtractor : IGuidelineRuleExtractor
{
    private const int MaxPromptChars = 60_000;

    private readonly AzureAiClient _ai;
    private readonly ILogger<AzureAiGuidelineRuleExtractor> _log;

    public AzureAiGuidelineRuleExtractor(AzureAiClient ai, ILogger<AzureAiGuidelineRuleExtractor> log)
    {
        _ai = ai;
        _log = log;
    }

    public async Task<GuidelineExtraction> ExtractAsync(string moduleId, string fileName, Stream content, CancellationToken ct)
    {
        try
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            var text = await _ai.ReadTextAsync(ms.ToArray(), ct);
            if (string.IsNullOrWhiteSpace(text))
                return new GuidelineExtraction([], "No text could be read from the document; add rules manually.");

            var truncated = text.Length > MaxPromptChars;
            var rules = await ProposeRulesAsync(moduleId, truncated ? text[..MaxPromptChars] : text, ct);
            return new GuidelineExtraction(rules, truncated ? "Document was long; only the first part was analysed. Review for completeness." : null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or AuthenticationFailedException or TaskCanceledException)
        {
            _log.LogWarning(ex, "Guideline AI extraction failed for {File}", fileName);
            return new GuidelineExtraction([], "AI extraction failed; add rules manually.");
        }
    }

    private async Task<IReadOnlyList<GuidelineRule>> ProposeRulesAsync(string moduleId, string text, CancellationToken ct)
    {
        using var parsed = await _ai.ChatJsonAsync(
            "You convert regulatory guideline documents into checklist rules used to validate CTD submission documents. " +
            "Return JSON: {\"rules\":[{\"text\":string,\"severity\":\"Low|Medium|High\",\"nodeRef\":string|null}]}. " +
            "Each rule must be a single verifiable statement taken from the guideline; do not invent requirements. At most 60 rules.",
            $"CTD module: {moduleId}\n\nGuideline text:\n{text}", ct);

        var rules = new List<GuidelineRule>();
        if (parsed.RootElement.TryGetProperty("rules", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in arr.EnumerateArray())
            {
                var t = r.TryGetProperty("text", out var tp) ? tp.GetString() : null;
                if (string.IsNullOrWhiteSpace(t)) continue;
                var sev = r.TryGetProperty("severity", out var sp) ? sp.GetString() : null;
                sev = GuidelinesRepository.Severities.FirstOrDefault(s => string.Equals(s, sev, StringComparison.OrdinalIgnoreCase)) ?? "Medium";
                var node = r.TryGetProperty("nodeRef", out var np) && np.ValueKind == JsonValueKind.String ? np.GetString() : null;
                rules.Add(new GuidelineRule($"R{rules.Count + 1:D3}", t.Trim(), sev, node));
                if (rules.Count == 60) break;
            }
        }
        return rules;
    }
}
