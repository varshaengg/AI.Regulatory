using AI.Regulatory.API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AI.Regulatory.API.Controllers;

/// <summary>BFF-scoped endpoints — SPA bootstrap and current user.</summary>
[ApiController]
[Route("api/v1/bff")]
[Tags("BFF")]
public sealed class BffController : ControllerBase
{
    private readonly IConfiguration _cfg;

    public BffController(IConfiguration cfg) => _cfg = cfg;

    /// <summary>SPA runtime bootstrap — tenant, api base, feature flags, branding.</summary>
    [AllowAnonymous]
    [HttpGet("config")]
    [ProducesResponseType(typeof(BffConfig), StatusCodes.Status200OK)]
    public ActionResult<BffConfig> GetConfig()
    {
        var tenantId = _cfg["EntraId:TenantId"] ?? "unknown";
        var branding = new BrandingTokens(
            ProductName: _cfg["Branding:ProductName"] ?? "Regulatory AI Assistant",
            PrimaryColor: _cfg["Branding:PrimaryColor"] ?? "#0F6CBD",
            LogoUrl: _cfg["Branding:LogoUrl"]);

        var flags = new Dictionary<string, bool>
        {
            ["copilotStreaming"] = true,
            ["gapAnalysis"] = true,
        };

        return Ok(new BffConfig(tenantId, "/api/v1", flags, branding));
    }

    /// <summary>Current authenticated user.</summary>
    [Authorize]
    [HttpGet("user")]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserProfile> GetCurrentUser()
    {
        var id = User.FindFirstValue("oid")
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? string.Empty;
        var email = User.FindFirstValue("preferred_username")
                    ?? User.FindFirstValue(ClaimTypes.Email)
                    ?? string.Empty;
        var name = User.FindFirstValue("name") ?? email;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Ok(new UserProfile(id, name, email, roles, AvatarUrl: null));
    }
}
