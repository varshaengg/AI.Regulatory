using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>
/// Tenant-wide default source configuration — A7 (Admin). Sets the fallback
/// source used for module analysis when a project has not overridden it via
/// its own <c>ProjectSource</c> row (see <see cref="ProjectSourcesController"/>).
/// </summary>
[ApiController]
[Route("api/v1/admin/sources")]
[Authorize(Policy = AuthPolicies.GlobalSourcesRead)]
[Tags("Global sources")]
[Produces("application/json")]
public sealed class GlobalSourcesController : ControllerBase
{
    private readonly GlobalSourcesRepository _repo;
    private readonly ISourceConnectivityValidator _connectivity;

    public GlobalSourcesController(GlobalSourcesRepository repo, ISourceConnectivityValidator connectivity)
    {
        _repo = repo;
        _connectivity = connectivity;
    }

    /// <summary>All CTD modules with their tenant default source, if any is configured.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GlobalSource?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<object>>> List(CancellationToken ct)
    {
        var defaults = await _repo.ListAsync(ct);
        var result = CtdModuleCatalog.All.Select(m => new
        {
            moduleId = m.Id,
            label = m.Label,
            color = m.Color,
            source = defaults.FirstOrDefault(g => string.Equals(g.ModuleId, m.Id, StringComparison.OrdinalIgnoreCase)),
        });
        return Ok(result);
    }

    /// <summary>Validate a candidate location before saving it as a module's default.</summary>
    [HttpPost("test")]
    [Authorize(Policy = AuthPolicies.GlobalSourcesAdmin)]
    [ProducesResponseType(typeof(ConnectionTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectionTestResult>> TestCandidate(
        [FromBody] TestSourceConnectionRequest req, CancellationToken ct)
        => Ok(await _connectivity.TestAsync(req.Type, req.Path, ct));

    /// <summary>Re-run the connectivity check for a module's existing default and persist the outcome.</summary>
    [HttpPost("{moduleId}/test")]
    [Authorize(Policy = AuthPolicies.GlobalSourcesAdmin)]
    [ProducesResponseType(typeof(ConnectionTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConnectionTestResult>> TestExisting(string moduleId, CancellationToken ct)
    {
        var existing = await _repo.ByModuleAsync(moduleId, ct);
        if (existing is null)
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Default source not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No default source configured for module '{moduleId}'.");
        }

        var result = await _connectivity.TestAsync(existing.Type, existing.Path, ct);
        await _repo.SetTestResultAsync(moduleId, result, ct);
        return Ok(result);
    }

    /// <summary>Set (create or replace) the tenant default for a module.</summary>
    [HttpPut("{moduleId}")]
    [Authorize(Policy = AuthPolicies.GlobalSourcesAdmin)]
    [ProducesResponseType(typeof(GlobalSource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GlobalSource>> Upsert(
        string moduleId, [FromBody] UpsertGlobalSourceRequest req, CancellationToken ct)
    {
        var validation = Validate(moduleId, req.Label, req.Path, req.Type);
        if (validation is not null) return validation;

        // Test connectivity up front so the default is saved with an accurate status (FR-010).
        var probe = await _connectivity.TestAsync(req.Type.Trim(), req.Path.Trim(), ct);
        var saved = await _repo.UpsertAsync(moduleId, req, probe, ct);
        return Ok(saved);
    }

    /// <summary>Remove the tenant default for a module (module reverts to "no default").</summary>
    [HttpDelete("{moduleId}")]
    [Authorize(Policy = AuthPolicies.GlobalSourcesAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string moduleId, CancellationToken ct)
        => await _repo.DeleteAsync(moduleId, ct) ? NoContent() : NotFound();

    private ActionResult<GlobalSource>? Validate(string moduleId, string label, string path, string type)
    {
        if (!CtdModuleCatalog.All.Any(m => string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase)))
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
