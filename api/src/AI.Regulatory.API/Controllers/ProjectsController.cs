using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>
/// M2 — Projects. API-Design §11.3. Data comes from <see cref="ProjectsRepository"/>,
/// which returns either seed data or (in the future) EF Core-backed records depending
/// on the <c>Data:IsMocked</c> configuration flag.
/// </summary>
[ApiController]
[Route("api/v1/projects")]
[Authorize]
[Tags("Projects (M2)")]
[Produces("application/json")]
public sealed class ProjectsController : ControllerBase
{
    private readonly ProjectsRepository _projects;

    public ProjectsController(ProjectsRepository projects) => _projects = projects;

    [HttpGet(Name = "ListProjects")]
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
    [Authorize(Policy = AuthPolicies.RaLeadOrAdmin)]
    [ProducesResponseType(typeof(ProjectDetail), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDetail>> Create(
        [FromBody] CreateProjectRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Country))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "name and country are required.");
        }

        var id = "prj-" + Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;
        var project = new ProjectDetail(
            Id: id,
            Name: req.Name,
            Country: req.Country,
            Status: "Draft",
            Product: req.Product ?? string.Empty,
            Modules: Array.Empty<string>(),
            OwnerEmail: "unknown@example.com",
            OwnerDisplayName: "Unknown",
            ProgressPct: 0,
            CreatedAt: now, UpdatedAt: now, Etag: "\"1\"");
        await _projects.AddAsync(project, ct);
        return CreatedAtRoute("GetProject", new { id }, project);
    }
}
