using System.Security.Claims;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>App users + their personas — A5.</summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
[Tags("Users (A5)")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly AppUsersRepository _users;
    public UsersController(AppUsersRepository users) => _users = users;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppUser>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> List(CancellationToken ct)
        => Ok(await _users.ListAsync(ct));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AppUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppUser>> Get(string id, CancellationToken ct)
    {
        var user = await _users.GetAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppUser), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppUser>> Create(
        [FromBody] CreateAppUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.AadObjectId) || string.IsNullOrWhiteSpace(req.Email))
            return ValidationProblem("AadObjectId and Email are required.");
        var addedBy = User.FindFirstValue("preferred_username") ?? User.Identity?.Name ?? "system";
        var user = await _users.Create(req, addedBy, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id}/personas")]
    [ProducesResponseType(typeof(AppUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppUser>> AssignPersonas(
        string id, [FromBody] AssignPersonasRequest req, CancellationToken ct)
    {
        var updated = await _users.AssignPersonas(id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
        => await _users.Delete(id, ct) ? NoContent() : NotFound();
}
