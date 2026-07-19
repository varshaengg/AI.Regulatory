using AI.Regulatory.API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Liveness / readiness probes — API-Design §11.1.</summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
[Tags("Health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Liveness probe — always 200 while the process is running.</summary>
    [HttpGet("live")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Live() => Ok(new { status = "ok" });

    /// <summary>Readiness probe — returns 200 once dependencies are reachable.</summary>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Ready()
    {
        // TODO: probe SQL, Blob, Key Vault, AI Search, Service Bus.
        return Ok(new { status = "ok", checks = Array.Empty<object>() });
    }
}
