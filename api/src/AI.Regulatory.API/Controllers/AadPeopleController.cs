using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>
/// Customer AD people-picker proxy. The SPA calls this from the A5 add-user
/// flow to search only the customer tenant — never the wider directory graph.
/// See <see cref="AadPeopleRepository"/> for live-vs-mocked semantics.
/// </summary>
[ApiController]
[Route("api/v1/aad")]
[Authorize]
[Tags("People (A5)")]
[Produces("application/json")]
public sealed class AadPeopleController : ControllerBase
{
    private readonly AadPeopleRepository _repo;
    public AadPeopleController(AadPeopleRepository repo) => _repo = repo;

    /// <summary>Search by display name or email prefix. Returns up to <paramref name="top"/> hits (max 25).</summary>
    [HttpGet("people")]
    [ProducesResponseType(typeof(IReadOnlyList<AadPerson>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AadPerson>>> Search(
        [FromQuery] string search, [FromQuery] int top = 10, CancellationToken ct = default)
        => Ok(await _repo.Search(search ?? string.Empty, top, ct));
}
