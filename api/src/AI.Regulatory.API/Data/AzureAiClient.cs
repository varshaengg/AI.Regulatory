using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace AI.Regulatory.API.Data;

/// <summary>Thin managed-identity client for Document Intelligence (text read) and Azure OpenAI (JSON chat).</summary>
public sealed class AzureAiClient
{
    private static readonly TokenCredential Credential = new DefaultAzureCredential();
    private static readonly string[] Scope = ["https://cognitiveservices.azure.com/.default"];

    private readonly HttpClient _http;
    private readonly string _diEndpoint;
    private readonly string _aoaiEndpoint;
    private readonly string _deployment;

    public AzureAiClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _diEndpoint = cfg["DocumentIntelligence:Endpoint"]!.TrimEnd('/');
        _aoaiEndpoint = cfg["AzureOpenAi:Endpoint"]!.TrimEnd('/');
        _deployment = cfg["AzureOpenAi:ChatDeployment"] ?? "gpt-4.1-mini";
    }

    public static bool IsConfigured(IConfiguration cfg)
        => !string.IsNullOrWhiteSpace(cfg["DocumentIntelligence:Endpoint"])
           && !string.IsNullOrWhiteSpace(cfg["AzureOpenAi:Endpoint"]);

    public async Task<string> ReadTextAsync(byte[] content, CancellationToken ct)
    {
        var url = $"{_diEndpoint}/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30";
        using var start = await SendAsync(HttpMethod.Post, url, new { base64Source = Convert.ToBase64String(content) }, ct);
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

    /// <summary>Chat completion constrained to a JSON object; returns the parsed JSON (caller disposes).</summary>
    public async Task<JsonDocument> ChatJsonAsync(string system, string user, CancellationToken ct)
    {
        var url = $"{_aoaiEndpoint}/openai/deployments/{_deployment}/chat/completions?api-version=2024-10-21";
        var body = new
        {
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            },
        };
        using var resp = await SendAsync(HttpMethod.Post, url, body, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var json = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
        return JsonDocument.Parse(json);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var token = await Credential.GetTokenAsync(new TokenRequestContext(Scope), ct);
        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await _http.SendAsync(req, ct);
    }
}
