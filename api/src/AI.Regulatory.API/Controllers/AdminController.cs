using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Admin overview — A1.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize]
[Tags("Admin")]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminRepository _repo;
    public AdminController(AdminRepository repo) => _repo = repo;

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStats), StatusCodes.Status200OK)]
    public ActionResult<AdminStats> GetStats() => Ok(_repo.GetStats());

    [HttpGet("activity")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminActivity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminActivity>>> RecentActivity(CancellationToken ct)
        => Ok(await _repo.ListAsync(ct));
}
