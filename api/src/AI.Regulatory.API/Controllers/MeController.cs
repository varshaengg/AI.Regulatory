using AI.Regulatory.API.Contracts;
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
}
