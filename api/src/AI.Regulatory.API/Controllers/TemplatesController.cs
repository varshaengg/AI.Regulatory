using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>CTD template catalog — A2/A3 admin screens.</summary>
[ApiController]
[Route("api/v1/templates")]
[Authorize]
[Tags("Templates (M3)")]
[Produces("application/json")]
public sealed class TemplatesController : ControllerBase
{
    private readonly TemplatesRepository _repo;
    public TemplatesController(TemplatesRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(Page<CtdTemplate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Page<CtdTemplate>>> List(CancellationToken ct)
    {
        var items = await _repo.ListAsync(ct);
        return Ok(new Page<CtdTemplate>(items, new PageInfo(items.Count, null, false)));
    }
}
