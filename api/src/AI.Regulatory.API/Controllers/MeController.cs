using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AI.Regulatory.API.Controllers;

/// <summary>M1 — authenticated user profile. API-Design §11.2.</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
[Tags("Me (M1)")]
public sealed class MeController : ControllerBase
{
    private readonly AppUsersRepository _users;
    private readonly PermissionMatrixRepository _matrix;

    public MeController(AppUsersRepository users, PermissionMatrixRepository matrix)
    {
        _users = users;
        _matrix = matrix;
    }

    /// <summary>Full profile including roles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    public ActionResult<UserProfile> Get()
    {
        var id = User.FindFirstValue("oid") ?? string.Empty;
        var email = User.FindFirstValue("preferred_username") ?? string.Empty;
        var name = User.FindFirstValue("name") ?? email;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(new UserProfile(id, name, email, roles, AvatarUrl: null));
    }

    /// <summary>
    /// Effective permissions for the caller — feeds the SPA's <c>usePermissions</c>
    /// hook that drives left-nav gating and per-screen action gating (A5/A6).
    /// </summary>
    /// <remarks>
    /// Persona resolution order:
    /// 1. If the caller matches an <see cref="AppUser"/> by AAD objectId, use their assigned personas.
    /// 2. Else fall back to <c>ClaimTypes.Role</c> values (dev/mock convenience).
    /// If neither resolves, an empty grants list is returned and the SPA
    /// treats the caller as unauthorised for every feature.
    /// </remarks>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(MePermissions), StatusCodes.Status200OK)]
    public async Task<ActionResult<MePermissions>> Permissions(CancellationToken ct)
    {
        var oid = User.FindFirstValue("oid");
        IReadOnlyList<string> personas = Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(oid))
        {
            var appUser = await _users.GetAsync(oid, ct);
            if (appUser is not null) personas = appUser.PersonaCodes;
        }
        if (personas.Count == 0)
            personas = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        var grants = await _matrix.GetEffectivePermissions(personas, ct);
        return Ok(new MePermissions(personas, grants));
    }
}

