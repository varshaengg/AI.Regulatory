using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Per-project source configuration — A4. Create/update/delete require write access;
/// connectivity checks follow SDD §4.4 FR-010 via <see cref="ISourceConnectivityValidator"/>.</summary>
[ApiController]
[Route("api/v1/projects/{projectId}/sources")]
[Authorize(Policy = AuthPolicies.DossierManagementRead)]
[Tags("Project sources")]
[Produces("application/json")]
public sealed class ProjectSourcesController : ControllerBase
{
    private readonly ProjectSourcesRepository _repo;
    private readonly ISourceConnectivityValidator _connectivity;

    public ProjectSourcesController(ProjectSourcesRepository repo, ISourceConnectivityValidator connectivity)
    {
        _repo = repo;
        _connectivity = connectivity;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSourcesByModule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSourcesByModule>>> ByProject(string projectId, CancellationToken ct)
        => Ok(await _repo.ByProjectAsync(projectId, ct));

    /// <summary>Validate a candidate source location before it is saved (FR-010).</summary>
    [HttpPost("test")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(ConnectionTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectionTestResult>> TestCandidate(
        string projectId, [FromBody] TestSourceConnectionRequest req, CancellationToken ct)
        => Ok(await _connectivity.TestAsync(req.Type, req.Path, ct));

    /// <summary>Re-run the connectivity check for an already-saved source and persist the outcome.</summary>
    [HttpPost("{id}/test")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(ConnectionTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConnectionTestResult>> TestExisting(string projectId, int id, CancellationToken ct)
    {
        var existing = await _repo.GetAsync(id.ToString(), ct);
        if (existing is null || !string.Equals(existing.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Source not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No source '{id}' on project '{projectId}'.");
        }

        var result = await _connectivity.TestAsync(existing.Type, existing.Path, ct);
        await _repo.SetTestResultAsync(projectId, id.ToString(), result, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(ProjectSource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectSource>> Create(
        string projectId, [FromBody] CreateProjectSourceRequest req, CancellationToken ct)
    {
        var validation = Validate(req.ModuleId, req.Label, req.Path, req.Type);
        if (validation is not null) return validation;

        // Test connectivity up front so the source is created with an accurate
        // initial status instead of a placeholder "unknown" state (FR-010).
        var probe = await _connectivity.TestAsync(req.Type.Trim(), req.Path.Trim(), ct);
        var source = new ProjectSource(
            Id: 0,
            ProjectId: projectId,
            ModuleId: req.ModuleId.Trim(),
            Label: req.Label.Trim(),
            Path: req.Path.Trim(),
            Type: req.Type.Trim(),
            SyncedAt: probe.TestedAt,
            Status: probe.Status);

        var created = await _repo.AddAsync(source, ct);
        return CreatedAtAction(nameof(ByProject), new { projectId }, created);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(ProjectSource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectSource>> Update(
        string projectId, int id, [FromBody] UpdateProjectSourceRequest req, CancellationToken ct)
    {
        var validation = Validate(null, req.Label, req.Path, req.Type);
        if (validation is not null) return validation;

        var updated = await _repo.UpdateAsync(projectId, id.ToString(), req, ct);
        if (updated is null)
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Source not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No source '{id}' on project '{projectId}'.");
        }

        // Re-validate connectivity so status/syncedAt reflect the edited path/type.
        var probe = await _connectivity.TestAsync(updated.Type, updated.Path, ct);
        var refreshed = await _repo.SetTestResultAsync(projectId, id.ToString(), probe, ct);
        return Ok(refreshed ?? updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string projectId, int id, CancellationToken ct)
        => await _repo.DeleteAsync(projectId, id.ToString(), ct) ? NoContent() : NotFound();

    private ActionResult<ProjectSource>? Validate(string? moduleId, string label, string path, string type)
    {
        if (moduleId is not null && !CtdModuleCatalog.All.Any(m => string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"moduleId must be one of {string.Join(", ", CtdModuleCatalog.All.Select(m => m.Id))}.");
        }

        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(type))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "label, path, and type are required.");
        }

        if (type is not ("Azure Blob" or "SharePoint"))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "type must be 'Azure Blob' or 'SharePoint'.");
        }

        return null;
    }
}

/// <summary>CTD module catalog + sub-module coverage — L4.</summary>
[ApiController]
[Route("api/v1/modules")]
[Authorize(Policy = AuthPolicies.DossierManagementRead)]
[Tags("CTD modules")]
[Produces("application/json")]
public sealed class ModulesController : ControllerBase
{
    private readonly SubModulesRepository _submodules;
    public ModulesController(SubModulesRepository submodules) => _submodules = submodules;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CtdModule>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CtdModule>> ListModules() => Ok(CtdModuleCatalog.All);

    [HttpGet("{moduleId}/submodules")]
    [ProducesResponseType(typeof(IReadOnlyList<SubModule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubModule>>> ListSubModules(string moduleId, CancellationToken ct)
    {
        // Filter seeded sub-modules by module prefix (3.2.S / 3.2.P belong to M3).
        var all = await _submodules.ListAsync(ct);
        var filtered = moduleId.Equals("M3", StringComparison.OrdinalIgnoreCase)
            ? all.Where(s => s.Code.StartsWith("3.2.")).ToArray()
            : (IReadOnlyList<SubModule>)Array.Empty<SubModule>();
        return Ok(filtered);
    }
}
