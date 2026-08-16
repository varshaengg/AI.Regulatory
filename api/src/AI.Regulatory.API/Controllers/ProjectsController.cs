using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AI.Regulatory.API.Controllers;

/// <summary>
/// M2 — Projects. API-Design §11.3. Data comes from <see cref="ProjectsRepository"/>,
/// which returns either seed data or (in the future) EF Core-backed records depending
/// on the <c>Data:IsMocked</c> configuration flag.
/// </summary>
[ApiController]
[Route("api/v1/projects")]
[Tags("Projects (M2)")]
[Produces("application/json")]
public sealed class ProjectsController : ControllerBase
{
    private readonly ProjectsRepository _projects;

    public ProjectsController(ProjectsRepository projects) => _projects = projects;

    [HttpGet(Name = "ListProjects")]
    [Authorize(Policy = AuthPolicies.DossierManagementRead)]
    [ProducesResponseType(typeof(Page<ProjectSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Page<ProjectSummary>>> List(
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken ct)
    {
        var size = Math.Clamp(pageSize ?? 50, 1, 200);
        var all = await _projects.ListAsync(ct);
        var items = all
            .Select(p => new ProjectSummary(p.Id, p.Name, p.Country, p.Status,
                                            p.Product, p.Modules, p.OwnerDisplayName,
                                            p.ProgressPct, p.CreatedAt))
            .Take(size)
            .ToList();
        return Ok(new Page<ProjectSummary>(items, new PageInfo(size, null, false)));
    }

    [HttpGet("{id}", Name = "GetProject")]
    [Authorize(Policy = AuthPolicies.DossierManagementRead)]
    [ProducesResponseType(typeof(ProjectDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetail>> Get(string id, CancellationToken ct)
    {
        var p = await _projects.GetAsync(id, ct);
        if (p is null)
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Project not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No project with id '{id}'.");
        }
        return Ok(p);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(ProjectDetail), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDetail>> Create(
        [FromBody] CreateProjectRequest req,
        CancellationToken ct)
    {
        var validation = Validate(req.Name, req.Country, req.Procedure);
        if (validation is not null) return validation;

        var now = DateTime.UtcNow;
        var ownerEmail = User.FindFirstValue("preferred_username") ?? User.Identity?.Name ?? "unknown@example.com";
        var ownerDisplayName = string.IsNullOrWhiteSpace(req.OwnerDisplayName) ? ownerEmail : req.OwnerDisplayName.Trim();
        var project = new ProjectDetail(
            Id: string.Empty,
            Name: req.Name.Trim(),
            Country: req.Country.Trim().ToUpperInvariant(),
            Status: "Draft",
            Product: req.Product?.Trim() ?? string.Empty,
            ProductVersion: req.ProductVersion?.Trim() ?? string.Empty,
            Procedure: req.Procedure?.Trim() ?? "Initial",
            TargetSubmissionDate: req.TargetSubmissionDate,
            Modules: Array.Empty<string>(),
            OwnerEmail: ownerEmail,
            OwnerDisplayName: ownerDisplayName,
            ProgressPct: 0,
            CreatedAt: now, UpdatedAt: now, Etag: "\"1\"");
        var created = await _projects.AddAsync(project, ct);
        return CreatedAtRoute("GetProject", new { id = created.Id }, created);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(ProjectDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<ProjectDetail>> Update(
        string id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] UpdateProjectRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return Problem(
                type: ErrorTypes.PreconditionFailed,
                title: "Precondition required",
                statusCode: StatusCodes.Status428PreconditionRequired,
                detail: "If-Match is required to update a project.");
        }

        var validation = Validate(req.Name, req.Country, req.Procedure);
        if (validation is not null) return validation;

        var updated = await _projects.UpdateAsync(id, req, ifMatch, ct);
        if (updated is not null) return Ok(updated);

        var existing = await _projects.GetAsync(id, ct);
        if (existing is null)
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Project not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No project with id '{id}'.");
        }

        return Problem(
            type: ErrorTypes.PreconditionFailed,
            title: "Project has changed",
            statusCode: StatusCodes.Status412PreconditionFailed,
            detail: "Reload the project before saving your changes.");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicies.DossierManagementAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(string id, CancellationToken ct)
        => await _projects.ArchiveAsync(id, ct) ? NoContent() : NotFound();

    private ActionResult<ProjectDetail>? Validate(string name, string country, string? procedure)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(country))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "name and country are required.");
        }

        if (!string.IsNullOrWhiteSpace(procedure)
            && procedure is not ("Initial" or "Variation" or "Renewal"))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "procedure must be Initial, Variation, or Renewal.");
        }

        return null;
    }
}
