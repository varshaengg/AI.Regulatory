using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Reviewer comments per project — R1.</summary>
[ApiController]
[Route("api/v1/projects/{projectId}/comments")]
[Authorize]
[Tags("Comments (M2)")]
[Produces("application/json")]
public sealed class CommentsController : ControllerBase
{
    private readonly CommentsRepository _repo;
    public CommentsController(CommentsRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Comment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Comment>>> List(string projectId, CancellationToken ct)
        => Ok(await _repo.ListAsync(ct));
}

/// <summary>Dossier runs — L6.</summary>
[ApiController]
[Route("api/v1/runs")]
[Authorize]
[Tags("Runs (M4)")]
[Produces("application/json")]
public sealed class RunsController : ControllerBase
{
    private readonly RunsRepository _repo;
    public RunsController(RunsRepository repo) => _repo = repo;

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DossierRun), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DossierRun>> Get(string id, CancellationToken ct)
    {
        var run = await _repo.GetAsync(id, ct);
        if (run is null)
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Run not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No dossier run with id '{id}'.");
        }
        return Ok(run);
    }
}

/// <summary>Compiled-dossier document tree — R1.</summary>
[ApiController]
[Route("api/v1/projects/{projectId}/doc-tree")]
[Authorize]
[Tags("Documents (M4)")]
[Produces("application/json")]
public sealed class DocTreeController : ControllerBase
{
    private readonly DocTreeRepository _repo;
    public DocTreeController(DocTreeRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocTreeNode>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocTreeNode>>> Get(string projectId, CancellationToken ct)
        => Ok(await _repo.ListAsync(ct));
}
