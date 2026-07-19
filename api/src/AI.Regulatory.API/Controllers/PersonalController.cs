using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Author assignments — U1.</summary>
[ApiController]
[Route("api/v1/me/assignments")]
[Authorize]
[Tags("Assignments (M2)")]
[Produces("application/json")]
public sealed class AssignmentsController : ControllerBase
{
    private readonly AssignmentsRepository _repo;
    public AssignmentsController(AssignmentsRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Assignment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Assignment>>> List(CancellationToken ct)
        => Ok(await _repo.ListAsync(ct));
}

/// <summary>User notifications — L1.</summary>
[ApiController]
[Route("api/v1/me/notifications")]
[Authorize]
[Tags("Notifications")]
[Produces("application/json")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationsRepository _repo;
    public NotificationsController(NotificationsRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserNotification>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserNotification>>> List(CancellationToken ct)
        => Ok(await _repo.ListAsync(ct));
}
