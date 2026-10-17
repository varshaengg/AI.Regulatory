using Azure.Identity;
using Azure.Storage.Blobs;
using AI.Regulatory.API.Contracts;

namespace AI.Regulatory.API.Data;

public sealed record DiscoveredDocument(
    string BlobPath,
    string FileName,
    string? ContentType,
    long SizeBytes,
    string? ETag,
    DateTime? LastModifiedUtc);

/// <summary>Lists documents under a source location. One implementation per source type.</summary>
public interface ISourceDocumentLister
{
    bool Supports(string sourceType);
    Task<IReadOnlyList<DiscoveredDocument>> ListAsync(string path, int maxItems, CancellationToken ct);
}

/// <summary>
/// Azure Blob lister. Source paths are "account/container[/prefix]"; access uses the workload
/// managed identity (DefaultAzureCredential), which needs Storage Blob Data Reader on the account.
/// </summary>
public sealed class BlobSourceDocumentLister : ISourceDocumentLister
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".txt", ".png", ".jpg", ".jpeg", ".tif", ".tiff",
    };

    private readonly DefaultAzureCredential _credential = new();

    public bool Supports(string sourceType) => string.Equals(sourceType, "Azure Blob", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<DiscoveredDocument>> ListAsync(string path, int maxItems, CancellationToken ct)
    {
        var (account, container, prefix) = Parse(path);
        var client = new BlobContainerClient(new Uri($"https://{account}.blob.core.windows.net/{container}"), _credential);
        var results = new List<DiscoveredDocument>();

        await foreach (var blob in client.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, prefix, ct))
        {
            if (!Extensions.Contains(System.IO.Path.GetExtension(blob.Name)))
                continue;

            results.Add(new DiscoveredDocument(
                $"{account}/{container}/{blob.Name}",
                System.IO.Path.GetFileName(blob.Name),
                blob.Properties.ContentType,
                blob.Properties.ContentLength ?? 0,
                blob.Properties.ETag?.ToString(),
                blob.Properties.LastModified?.UtcDateTime));

            if (results.Count >= maxItems)
                break;
        }

        return results;
    }

    internal static (string Account, string Container, string? Prefix) Parse(string path)
    {
        var segments = (path ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
            throw new ArgumentException("Path must be 'account/container[/prefix]'.", nameof(path));

        var prefix = segments.Length > 2 ? string.Join('/', segments.Skip(2)) + "/" : null;
        return (segments[0], segments[1], prefix);
    }
}
