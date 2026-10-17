using System.Security.Claims;
using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>AI-assisted dossier preparation runs.</summary>
[ApiController]
[Route("api/v1")]
[Authorize(Policy = AuthPolicies.DossierManagementRead)]
[Tags("Dossier preparation")]
[Produces("application/json")]
public sealed class PreparationRunsController : ControllerBase
{
    private readonly PreparationRunService _service;
    private readonly PreparationRunsRepository _repo;

    public PreparationRunsController(PreparationRunService service, PreparationRunsRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    /// <summary>Starts a run: resolves the country's templates and source locations and snapshots them.</summary>
    [HttpPost("projects/{projectId}/dossier-runs")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(PreparationRun), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PreparationRun>> Create(string projectId, CancellationToken ct)
    {
        var result = await _service.CreateAsync(projectId, CurrentUserName(), ct);
        switch (result.Outcome)
        {
            case PreparationRunOutcome.ProjectNotFound:
                return Problem(type: ErrorTypes.NotFound, title: "Project not found",
                    statusCode: StatusCodes.Status404NotFound, detail: $"No project with id '{projectId}'.");
            case PreparationRunOutcome.Created:
                return Created($"/api/v1/dossier-runs/{result.Run!.Id}", result.Run);
            default:
                return Problem(title: "Run cannot start",
                    statusCode: StatusCodes.Status422UnprocessableEntity, detail: result.Detail);
        }
    }

    [HttpGet("projects/{projectId}/dossier-runs")]
    [ProducesResponseType(typeof(IReadOnlyList<PreparationRun>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PreparationRun>>> List(string projectId, CancellationToken ct)
        => Ok(await _repo.ListByProjectAsync(projectId, ct));

    [HttpGet("dossier-runs/{id}")]
    [ProducesResponseType(typeof(PreparationRun), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreparationRun>> Get(string id, CancellationToken ct)
    {
        var run = await _repo.GetAsync(id, ct);
        return run is null ? RunNotFound(id) : Ok(run);
    }

    [HttpGet("dossier-runs/{id}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<PreparationRunEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PreparationRunEvent>>> Events(string id, CancellationToken ct)
    {
        if (await _repo.GetAsync(id, ct) is null)
            return RunNotFound(id);
        return Ok(await _repo.ListEventsAsync(id, ct));
    }

    /// <summary>Runs the discovery stage inline. The Service Bus worker will take this over.</summary>
    [HttpPost("dossier-runs/{id}/discover")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(typeof(IReadOnlyList<PreparationDocument>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PreparationDocument>>> Discover(string id, CancellationToken ct)
    {
        var documents = await _service.DiscoverAsync(id, ct);
        return documents is null ? RunNotFound(id) : Ok(documents);
    }

    [HttpGet("dossier-runs/{id}/documents")]
    [ProducesResponseType(typeof(IReadOnlyList<PreparationDocument>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PreparationDocument>>> Documents(string id, CancellationToken ct)
    {
        if (await _repo.GetAsync(id, ct) is null)
            return RunNotFound(id);
        return Ok(await _repo.ListDocumentsAsync(id, ct));
    }

    private ActionResult RunNotFound(string id)
        => Problem(type: ErrorTypes.NotFound, title: "Run not found",
            statusCode: StatusCodes.Status404NotFound, detail: $"No dossier run with id '{id}'.");

    private string CurrentUserName()
        => User.FindFirstValue("name")
            ?? User.FindFirstValue("preferred_username")
            ?? User.Identity?.Name
            ?? "unknown";
}
