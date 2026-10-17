using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI.Regulatory.API.Contracts;
using Azure.Core;
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

/// <summary>Document Intelligence (prebuilt-read) for text, then Azure OpenAI to propose rules. Managed identity only.</summary>
public sealed class AzureAiGuidelineRuleExtractor : IGuidelineRuleExtractor
{
    private const int MaxPromptChars = 60_000;
    private static readonly TokenCredential Credential = new DefaultAzureCredential();
    private static readonly string[] Scope = ["https://cognitiveservices.azure.com/.default"];

    private readonly HttpClient _http;
    private readonly string _diEndpoint;
    private readonly string _aoaiEndpoint;
    private readonly string _deployment;
    private readonly ILogger<AzureAiGuidelineRuleExtractor> _log;

    public AzureAiGuidelineRuleExtractor(HttpClient http, IConfiguration cfg, ILogger<AzureAiGuidelineRuleExtractor> log)
    {
        _http = http;
        _log = log;
        _diEndpoint = cfg["DocumentIntelligence:Endpoint"]!.TrimEnd('/');
        _aoaiEndpoint = cfg["AzureOpenAi:Endpoint"]!.TrimEnd('/');
        _deployment = cfg["AzureOpenAi:ChatDeployment"] ?? "gpt-4o-mini";
    }

    public async Task<GuidelineExtraction> ExtractAsync(string moduleId, string fileName, Stream content, CancellationToken ct)
    {
        try
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            var text = await ReadTextAsync(ms.ToArray(), ct);
            if (string.IsNullOrWhiteSpace(text))
                return new GuidelineExtraction([], "No text could be read from the document; add rules manually.");

            var rules = await ProposeRulesAsync(moduleId, text.Length > MaxPromptChars ? text[..MaxPromptChars] : text, ct);
            var note = text.Length > MaxPromptChars ? "Document was long; only the first part was analysed. Review for completeness." : null;
            return new GuidelineExtraction(rules, note);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or AuthenticationFailedException or TaskCanceledException)
        {
            _log.LogWarning(ex, "Guideline AI extraction failed for {File}", fileName);
            return new GuidelineExtraction([], "AI extraction failed; add rules manually.");
        }
    }

    private async Task<string> ReadTextAsync(byte[] pdf, CancellationToken ct)
    {
        var url = $"{_diEndpoint}/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30";
        using var start = await SendAsync(HttpMethod.Post, url, new { base64Source = Convert.ToBase64String(pdf) }, ct);
        start.EnsureSuccessStatusCode();
        var op = start.Headers.TryGetValues("Operation-Location", out var v) ? v.First()
            : throw new InvalidOperationException("Document Intelligence returned no Operation-Location.");

        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            using var poll = await SendAsync(HttpMethod.Get, op, null, ct);
            poll.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await poll.Content.ReadAsStringAsync(ct));
            var status = doc.RootElement.GetProperty("status").GetString();
            if (status == "succeeded")
                return doc.RootElement.GetProperty("analyzeResult").GetProperty("content").GetString() ?? "";
            if (status is "failed" or "canceled")
                throw new InvalidOperationException($"Document Intelligence analysis {status}.");
        }
        throw new InvalidOperationException("Document Intelligence analysis timed out.");
    }

    private async Task<IReadOnlyList<GuidelineRule>> ProposeRulesAsync(string moduleId, string text, CancellationToken ct)
    {
        var url = $"{_aoaiEndpoint}/openai/deployments/{_deployment}/chat/completions?api-version=2024-10-21";
        var body = new
        {
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content =
                    "You convert regulatory guideline documents into checklist rules used to validate CTD submission documents. " +
                    "Return JSON: {\"rules\":[{\"text\":string,\"severity\":\"Low|Medium|High\",\"nodeRef\":string|null}]}. " +
                    "Each rule must be a single verifiable statement taken from the guideline; do not invent requirements. At most 60 rules." },
                new { role = "user", content = $"CTD module: {moduleId}\n\nGuideline text:\n{text}" }
            }
        };
        using var resp = await SendAsync(HttpMethod.Post, url, body, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var json = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
        using var parsed = JsonDocument.Parse(json);

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

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var token = await Credential.GetTokenAsync(new TokenRequestContext(Scope), ct);
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await _http.SendAsync(req, ct);
    }
}
