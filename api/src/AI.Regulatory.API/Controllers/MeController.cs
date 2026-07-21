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
    private readonly IConfiguration _config;

    public MeController(AppUsersRepository users, PermissionMatrixRepository matrix, IConfiguration config)
    {
        _users = users;
        _matrix = matrix;
        _config = config;
    }

    /// <summary>Full profile including roles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    public ActionResult<UserProfile> Get()
    {
        var id = ResolveOid(User) ?? string.Empty;
        var email = User.FindFirstValue("preferred_username") ?? string.Empty;
        var name = User.FindFirstValue("name") ?? email;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(new UserProfile(id, name, email, roles, AvatarUrl: null));
    }

    // Microsoft.Identity.Web maps the short "oid" claim to the long-form URI
    // "http://schemas.microsoft.com/identity/claims/objectidentifier" via
    // JwtBearer's default inbound claim mapping. Read both forms so we work
    // regardless of the mapping setting.
    private static string? ResolveOid(ClaimsPrincipal user) =>
        user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? user.FindFirstValue("oid")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Effective permissions for the caller — feeds the SPA's <c>usePermissions</c>
    /// hook that drives left-nav gating and per-screen action gating (A5/A6).
    /// </summary>
    /// <remarks>
    /// Persona resolution order:
    /// 1. If the caller's <c>oid</c> is listed in <c>Admin:BootstrapOids</c>, treat as full Admin
    ///    (bootstrap escape hatch — required before any AppUser can be enrolled through A5).
    /// 2. If the caller matches an <see cref="AppUser"/> by AAD objectId, use their assigned personas.
    /// 3. Else fall back to <c>ClaimTypes.Role</c> values (dev/mock convenience).
    /// If neither resolves, an empty grants list is returned and the SPA
    /// treats the caller as unauthorised for every feature.
    /// </remarks>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(MePermissions), StatusCodes.Status200OK)]
    public async Task<ActionResult<MePermissions>> Permissions(CancellationToken ct)
    {
        var oid = ResolveOid(User);
        IReadOnlyList<string> personas = Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(oid))
        {
            var bootstrap = _config.GetSection("Admin:BootstrapOids").Get<string[]>() ?? Array.Empty<string>();
            if (bootstrap.Any(o => string.Equals(o, oid, StringComparison.OrdinalIgnoreCase)))
            {
                personas = new[] { "Admin" };
            }
            else
            {
                var appUser = await _users.GetAsync(oid, ct);
                if (appUser is not null) personas = appUser.PersonaCodes;
            }
        }
        if (personas.Count == 0)
            personas = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        var grants = await _matrix.GetEffectivePermissions(personas, ct);
        return Ok(new MePermissions(personas, grants));
    }
}

