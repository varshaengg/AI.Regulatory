using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Persona catalogue — feeds A5 add-user dialog + A6 matrix.</summary>
[ApiController]
[Route("api/v1/personas")]
[Authorize]
[Tags("Personas (A5/A6)")]
[Produces("application/json")]
public sealed class PersonasController : ControllerBase
{
    private readonly PersonasRepository _repo;
    public PersonasController(PersonasRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Persona>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Persona>>> List(CancellationToken ct)
        => Ok(await _repo.ListAsync(ct));
}
