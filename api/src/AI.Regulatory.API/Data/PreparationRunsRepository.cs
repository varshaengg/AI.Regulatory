using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using AI.Regulatory.API.Contracts;
using Dapper;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Persistence for AI dossier preparation runs and their event log (DossierRun, DossierRunEvent).</summary>
public sealed class PreparationRunsRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] StatusNames = ["Queued", "Running", "Completed", "Failed"];

    private readonly ISqlConnectionFactory _sql;
    private readonly bool _isMocked;
    private readonly ConcurrentDictionary<string, PreparationRun> _runs = new();
    private readonly ConcurrentDictionary<string, List<PreparationRunEvent>> _events = new();
    private readonly ConcurrentDictionary<string, List<PreparationDocument>> _documents = new();

    public PreparationRunsRepository(IOptions<DataOptions> options, ISqlConnectionFactory sql)
    {
        _isMocked = options.Value.IsMocked;
        _sql = sql;
    }

    public async Task<PreparationRun> CreateAsync(
        string projectId,
        string country,
        string startedBy,
        PreparationSnapshot snapshot,
        IReadOnlyList<PreparationRunEvent> events,
        CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var run = new PreparationRun(id.ToString(), projectId, StatusNames[0], country, startedBy, now, null, snapshot);

        if (_isMocked)
        {
            _runs[run.Id] = run;
            _events[run.Id] = events.ToList();
            return run;
        }

        var projectGuid = await ResolveProjectGuidAsync(projectId, ct)
            ?? throw new InvalidOperationException($"Project '{projectId}' not found.");

        await using var c = await _sql.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO [dbo].[DossierRun] ([Id], [ProjectId], [Country], [Status], [StartedUtc], [StartedBy], [Snapshot])
            VALUES (@id, @projectGuid, @country, 0, @now, @startedBy, @snapshot);
            """,
            new { id, projectGuid, country, now, startedBy, snapshot = JsonSerializer.Serialize(snapshot, Json) },
            tx, cancellationToken: ct));

        foreach (var e in events)
        {
            await c.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO [dbo].[DossierRunEvent] ([DossierRunId], [OccurredUtc], [Stage], [Severity], [Message])
                VALUES (@id, @OccurredUtc, @Stage, @Severity, @Message);
                """,
                new { id, e.OccurredUtc, e.Stage, e.Severity, e.Message },
                tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return run;
    }

    public async Task<PreparationRun?> GetAsync(string id, CancellationToken ct)
    {
        if (_isMocked)
            return _runs.TryGetValue(id, out var run) ? run : null;

        if (!Guid.TryParse(id, out var guid))
            return null;

        await using var c = await _sql.OpenAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<RunRow>(new CommandDefinition(RunSelect + " WHERE r.[Id] = @guid;", new { guid }, cancellationToken: ct));
        return row is null ? null : ToRun(row);
    }

    public async Task<IReadOnlyList<PreparationRun>> ListByProjectAsync(string projectId, CancellationToken ct)
    {
        if (_isMocked)
        {
            return _runs.Values
                .Where(r => string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.StartedUtc)
                .ToArray();
        }

        if (!int.TryParse(projectId, NumberStyles.None, CultureInfo.InvariantCulture, out var projectNumber))
            return Array.Empty<PreparationRun>();

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<RunRow>(new CommandDefinition(
            RunSelect + " WHERE p.[ProjectNumber] = @projectNumber ORDER BY r.[StartedUtc] DESC;",
            new { projectNumber }, cancellationToken: ct));
        return rows.Select(ToRun).ToArray();
    }

    public async Task<IReadOnlyList<PreparationRunEvent>> ListEventsAsync(string runId, CancellationToken ct)
    {
        if (_isMocked)
            return _events.TryGetValue(runId, out var list) ? list.ToArray() : Array.Empty<PreparationRunEvent>();

        if (!Guid.TryParse(runId, out var guid))
            return Array.Empty<PreparationRunEvent>();

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<PreparationRunEvent>(new CommandDefinition(
            """
            SELECT [OccurredUtc], [Stage], [Severity], [Message]
            FROM [dbo].[DossierRunEvent] WHERE [DossierRunId] = @guid ORDER BY [Id];
            """,
            new { guid }, cancellationToken: ct));
        return rows.ToArray();
    }

    public async Task AddEventsAsync(string runId, IReadOnlyList<PreparationRunEvent> events, CancellationToken ct)
    {
        if (events.Count == 0) return;

        if (_isMocked)
        {
            _events.GetOrAdd(runId, _ => new List<PreparationRunEvent>()).AddRange(events);
            return;
        }

        var guid = Guid.Parse(runId);
        await using var c = await _sql.OpenAsync(ct);
        foreach (var e in events)
        {
            await c.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO [dbo].[DossierRunEvent] ([DossierRunId], [OccurredUtc], [Stage], [Severity], [Message])
                VALUES (@guid, @OccurredUtc, @Stage, @Severity, LEFT(@Message, 1000));
                """,
                new { guid, e.OccurredUtc, e.Stage, e.Severity, e.Message }, cancellationToken: ct));
        }
    }

    public async Task SetStatusAsync(string runId, string status, CancellationToken ct)
    {
        var code = (byte)Array.IndexOf(StatusNames, status);
        if (_isMocked)
        {
            if (_runs.TryGetValue(runId, out var run))
                _runs[runId] = run with { Status = status };
            return;
        }

        await using var c = await _sql.OpenAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE [dbo].[DossierRun] SET [Status] = @code WHERE [Id] = @guid;",
            new { code, guid = Guid.Parse(runId) }, cancellationToken: ct));
    }

    /// <summary>Replaces the run's discovered documents, so discovery can be re-run safely.</summary>
    public async Task ReplaceDocumentsAsync(string runId, IReadOnlyList<PreparationDocument> documents, CancellationToken ct)
    {
        if (_isMocked)
        {
            _documents[runId] = documents.ToList();
            return;
        }

        var guid = Guid.Parse(runId);
        await using var c = await _sql.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM [dbo].[DossierDocument] WHERE [DossierRunId] = @guid;", new { guid }, tx, cancellationToken: ct));
        foreach (var d in documents)
        {
            await c.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO [dbo].[DossierDocument] ([DossierRunId], [ModuleId], [SourceLabel], [SourceType], [BlobPath], [FileName], [ContentType], [SizeBytes], [LastModifiedUtc], [Status])
                VALUES (@guid, @ModuleId, @SourceLabel, @SourceType, @BlobPath, @FileName, @ContentType, @SizeBytes, @LastModifiedUtc, @Status);
                """,
                new { guid, d.ModuleId, d.SourceLabel, d.SourceType, d.BlobPath, d.FileName, d.ContentType, d.SizeBytes, d.LastModifiedUtc, d.Status },
                tx, cancellationToken: ct));
        }
        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<PreparationDocument>> ListDocumentsAsync(string runId, CancellationToken ct)
    {
        if (_isMocked)
            return _documents.TryGetValue(runId, out var list) ? list.ToArray() : Array.Empty<PreparationDocument>();

        if (!Guid.TryParse(runId, out var guid))
            return Array.Empty<PreparationDocument>();

        await using var c = await _sql.OpenAsync(ct);
        var rows = await c.QueryAsync<PreparationDocument>(new CommandDefinition(
            """
            SELECT CONVERT(VARCHAR(36), [Id]) AS [Id], CONVERT(VARCHAR(36), [DossierRunId]) AS [RunId], [ModuleId], [SourceLabel], [SourceType],
                   [BlobPath], [FileName], [ContentType], [SizeBytes], [LastModifiedUtc], [Status]
            FROM [dbo].[DossierDocument] WHERE [DossierRunId] = @guid ORDER BY [ModuleId], [FileName];
            """,
            new { guid }, cancellationToken: ct));
        return rows.ToArray();
    }

    private const string RunSelect =
        """
        SELECT CONVERT(VARCHAR(36), r.[Id]) AS [Id], CONVERT(VARCHAR(20), p.[ProjectNumber]) AS [ProjectId],
               r.[Status], r.[Country], r.[StartedBy], r.[StartedUtc], r.[CompletedUtc], r.[Snapshot]
        FROM [dbo].[DossierRun] r
        JOIN [dbo].[Project] p ON p.[Id] = r.[ProjectId]
        """;

    private static PreparationRun ToRun(RunRow r)
    {
        var snapshot = string.IsNullOrWhiteSpace(r.Snapshot)
            ? new PreparationSnapshot(r.Country ?? string.Empty, Array.Empty<PreparationModuleSnapshot>())
            : JsonSerializer.Deserialize<PreparationSnapshot>(r.Snapshot, Json)!;
        var status = r.Status < StatusNames.Length ? StatusNames[r.Status] : "Unknown";
        return new PreparationRun(r.Id, r.ProjectId, status, r.Country ?? string.Empty, r.StartedBy, r.StartedUtc, r.CompletedUtc, snapshot);
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

    private sealed class RunRow
    {
        public string Id { get; init; } = string.Empty;
        public string ProjectId { get; init; } = string.Empty;
        public byte Status { get; init; }
        public string? Country { get; init; }
        public string StartedBy { get; init; } = string.Empty;
        public DateTime StartedUtc { get; init; }
        public DateTime? CompletedUtc { get; init; }
        public string? Snapshot { get; init; }
    }
}
