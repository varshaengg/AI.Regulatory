using AI.Regulatory.API.Contracts;

namespace AI.Regulatory.API.Data;

public enum PreparationRunOutcome { Created, ProjectNotFound, MissingCountry, NothingToProcess }

public sealed record PreparationRunResult(PreparationRunOutcome Outcome, PreparationRun? Run = null, string? Detail = null);

/// <summary>
/// Creates a preparation run for a project: resolves every module's template (project override, then
/// country default, then global default) and its source (project override, else tenant default), and
/// freezes the result as the run snapshot. Later stages (discovery, parsing) read the snapshot only.
/// </summary>
public sealed class PreparationRunService
{
    private readonly ProjectsRepository _projects;
    private readonly TemplatesRepository _templates;
    private readonly ProjectSourcesRepository _sources;
    private readonly PreparationRunsRepository _runs;
    private readonly GuidelinesRepository _guidelines;
    private readonly IDocumentValidator _validator;
    private readonly IEnumerable<ISourceDocumentLister> _listers;
    private readonly ILogger<PreparationRunService> _log;
    private const int MaxDocumentsPerSource = 500;
    private const int MaxDocumentsToValidate = 50;

    public PreparationRunService(
        ProjectsRepository projects,
        TemplatesRepository templates,
        ProjectSourcesRepository sources,
        PreparationRunsRepository runs,
        GuidelinesRepository guidelines,
        IDocumentValidator validator,
        IEnumerable<ISourceDocumentLister> listers,
        ILogger<PreparationRunService> log)
    {
        _listers = listers;
        _log = log;
        _projects = projects;
        _templates = templates;
        _sources = sources;
        _runs = runs;
        _guidelines = guidelines;
        _validator = validator;
    }

    /// <summary>
    /// Discovery stage: lists documents in every source of every module that has a template, as
    /// recorded in the run snapshot. A failing source is logged and skipped; it never fails the run.
    /// </summary>
    public async Task<IReadOnlyList<PreparationDocument>?> DiscoverAsync(string runId, CancellationToken ct)
    {
        var run = await _runs.GetAsync(runId, ct);
        if (run is null) return null;

        await _runs.SetStatusAsync(runId, "Running", ct);
        var documents = await DiscoverCoreAsync(run, ct);
        await _runs.SetStatusAsync(runId, "Completed", ct);
        return documents;
    }

    /// <summary>Full pipeline run by the worker: Discover, then Validate against the module guidelines.</summary>
    public async Task<bool> ProcessAsync(string runId, CancellationToken ct)
    {
        var run = await _runs.GetAsync(runId, ct);
        if (run is null) return false;

        await _runs.SetStatusAsync(runId, "Running", ct);
        await DiscoverCoreAsync(run, ct);
        await ValidateCoreAsync(run, ct);
        await _runs.SetStatusAsync(runId, "Completed", ct);
        return true;
    }

    /// <summary>Validate stage on its own, over the documents already discovered for the run.</summary>
    public async Task<IReadOnlyList<PreparationFinding>?> ValidateAsync(string runId, CancellationToken ct)
    {
        var run = await _runs.GetAsync(runId, ct);
        if (run is null) return null;

        await ValidateCoreAsync(run, ct);
        return await _runs.ListFindingsAsync(runId, ct);
    }

    private async Task<IReadOnlyList<PreparationDocument>> DiscoverCoreAsync(PreparationRun run, CancellationToken ct)
    {
        var runId = run.Id;
        var documents = new List<PreparationDocument>();
        var events = new List<PreparationRunEvent>();

        foreach (var module in run.Snapshot.Modules.Where(m => m.Template is not null))
        {
            foreach (var source in module.Sources)
            {
                var lister = _listers.FirstOrDefault(l => l.Supports(source.Type));
                if (lister is null)
                {
                    events.Add(new(DateTime.UtcNow, "Discover", "Warning", $"{module.ModuleId}: source '{source.Label}' type '{source.Type}' is not supported yet; skipped."));
                    continue;
                }

                try
                {
                    var found = await lister.ListAsync(source.Path, MaxDocumentsPerSource, ct);
                    documents.AddRange(found.Select(d => new PreparationDocument(
                        Guid.NewGuid().ToString(), runId, module.ModuleId, source.Label, source.Type,
                        d.BlobPath, d.FileName, d.ContentType, d.SizeBytes, d.LastModifiedUtc, "Discovered")));
                    events.Add(new(DateTime.UtcNow, "Discover", "Info", $"{module.ModuleId}: found {found.Count} document(s) in '{source.Label}'."));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Discovery failed for run {RunId} module {Module} source {Source}", runId, module.ModuleId, source.Label);
                    events.Add(new(DateTime.UtcNow, "Discover", "Error", $"{module.ModuleId}: could not read '{source.Label}' ({ex.GetType().Name})."));
                }
            }
        }

        await _runs.ReplaceDocumentsAsync(runId, documents, ct);
        events.Add(new(DateTime.UtcNow, "Discover", "Info", $"Discovery finished: {documents.Count} document(s)."));
        await _runs.AddEventsAsync(runId, events, ct);
        return documents;
    }

    /// <summary>
    /// Advisory validation: each discovered document is checked against its module's frozen guideline rules.
    /// Findings never block the run; a document that cannot be read is logged and skipped.
    /// </summary>
    private async Task ValidateCoreAsync(PreparationRun run, CancellationToken ct)
    {
        var withRules = run.Snapshot.Modules.Where(m => m.Guideline is { Rules.Count: > 0 }).ToArray();
        if (withRules.Length == 0)
        {
            await _runs.AddEventsAsync(run.Id, [new(DateTime.UtcNow, "Validate", "Info", "No published guideline applies to this run; validation skipped.")], ct);
            return;
        }
        if (!_validator.IsAvailable)
        {
            await _runs.AddEventsAsync(run.Id, [new(DateTime.UtcNow, "Validate", "Warning", "AI validation is not configured; validation skipped.")], ct);
            return;
        }

        var documents = await _runs.ListDocumentsAsync(run.Id, ct);
        var findings = new List<PreparationFinding>();
        var events = new List<PreparationRunEvent>();
        var checkedCount = 0;
        var limitReached = false;

        foreach (var module in withRules)
        {
            var candidates = documents.Where(d => d.ModuleId == module.ModuleId && AzureAiDocumentValidator.CanValidate(d));
            foreach (var doc in candidates)
            {
                if (checkedCount >= MaxDocumentsToValidate)
                {
                    limitReached = true;
                    break;
                }

                try
                {
                    findings.AddRange(await _validator.ValidateAsync(doc, module.Guideline!.Rules, ct));
                    checkedCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Validation failed for run {RunId} document {File}", run.Id, doc.FileName);
                    events.Add(new(DateTime.UtcNow, "Validate", "Error", $"{module.ModuleId}: could not validate '{doc.FileName}' ({ex.GetType().Name})."));
                }
            }
            if (limitReached) break;
        }

        if (limitReached)
            events.Add(new(DateTime.UtcNow, "Validate", "Warning", $"Validation limit of {MaxDocumentsToValidate} documents reached; remaining documents were not checked."));
        await _runs.ReplaceFindingsAsync(run.Id, findings, ct);
        var flagged = findings.Count(f => f.Verdict == "flag");
        events.Add(new(DateTime.UtcNow, "Validate", "Info", $"Validated {checkedCount} document(s): {flagged} flagged of {findings.Count} checks (advisory)."));
        await _runs.AddEventsAsync(run.Id, events, ct);
    }

    public async Task<PreparationRunResult> CreateAsync(string projectId, string startedBy, CancellationToken ct)
    {
        var project = await _projects.GetAsync(projectId, ct);
        if (project is null)
            return new PreparationRunResult(PreparationRunOutcome.ProjectNotFound);

        var country = project.Country?.Trim().ToUpperInvariant() ?? string.Empty;
        if (country.Length != 2)
            return new PreparationRunResult(PreparationRunOutcome.MissingCountry, Detail: "The project has no country set.");

        var templates = await _templates.ResolveForProjectAsync(projectId, ct);
        var sources = await _sources.ByProjectAsync(projectId, ct);

        var modules = CtdModuleCatalog.All.Select(m =>
        {
            var warnings = new List<string>();
            var entry = templates.FirstOrDefault(t => t.ModuleId == m.Id);
            var template = entry?.Template is { } t
                ? new PreparationTemplateRef(t.Id, t.FileName, t.Version, t.Country, t.Scope)
                : null;
            if (template is null)
                warnings.Add("No published template resolves for this module.");

            var moduleSources = (sources.FirstOrDefault(s => s.ModuleId == m.Id)?.Sources ?? Array.Empty<ProjectSource>())
                .Select(s => new PreparationSourceRef(s.Label, s.Path, s.Type, s.IsDefault))
                .ToArray();
            if (moduleSources.Length == 0)
                warnings.Add("No source location is configured for this module.");

            return new PreparationModuleSnapshot(m.Id, m.Label, template, moduleSources, warnings);
        }).ToArray();

        for (var i = 0; i < modules.Length; i++)
        {
            var g = await _guidelines.ResolvePublishedAsync(modules[i].ModuleId, country, ct);
            if (g is not null)
                modules[i] = modules[i] with { Guideline = new PreparationGuidelineRef(g.Id, g.Version, g.Country, g.Rules) };
        }

        if (modules.All(m => m.Template is null || m.Sources.Count == 0))
        {
            return new PreparationRunResult(
                PreparationRunOutcome.NothingToProcess,
                Detail: "No module has both a published template and a source location.");
        }

        var now = DateTime.UtcNow;
        var events = new List<PreparationRunEvent>
        {
            new(now, "Resolve", "Info", $"Run created for country {country}; {modules.Count(m => m.Template is not null && m.Sources.Count > 0)} of {modules.Length} modules are ready."),
        };
        events.AddRange(modules.SelectMany(m => m.Warnings.Select(w => new PreparationRunEvent(now, "Resolve", "Warning", $"{m.ModuleId}: {w}"))));

        var run = await _runs.CreateAsync(projectId, country, startedBy, new PreparationSnapshot(country, modules), events, ct);
        return new PreparationRunResult(PreparationRunOutcome.Created, run);
    }
}
