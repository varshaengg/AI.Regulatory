using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>
/// Feature × Permission catalog + persona-permission matrix — A6.
///
/// The matrix is exposed as a flat list of {persona, feature, permission}
/// grants; the SPA pivots it into a grid client-side. Toggles PUT one entry
/// at a time so partial failures don't leave the matrix in an unknown state.
/// </summary>
[ApiController]
[Route("api/v1/permissions")]
[Authorize]
[Tags("Permissions (A6)")]
[Produces("application/json")]
public sealed class PermissionsController : ControllerBase
{
    private readonly PermissionsRepository _permissions;
    private readonly FeaturesRepository _features;
    private readonly PermissionMatrixRepository _matrix;

    public PermissionsController(
        PermissionsRepository permissions,
        FeaturesRepository features,
        PermissionMatrixRepository matrix)
    {
        _permissions = permissions;
        _features = features;
        _matrix = matrix;
    }

    /// <summary>All permission verbs (Read/Write/Review/Admin).</summary>
    [HttpGet("verbs")]
    [ProducesResponseType(typeof(IReadOnlyList<Permission>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Permission>>> Verbs(CancellationToken ct)
        => Ok(await _permissions.ListAsync(ct));

    /// <summary>All features that participate in the matrix.</summary>
    [HttpGet("features")]
    [ProducesResponseType(typeof(IReadOnlyList<Feature>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Feature>>> Features(CancellationToken ct)
        => Ok(await _features.ListAsync(ct));

    /// <summary>Full matrix — sparse list of granted cells.</summary>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionMatrixEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionMatrixEntry>>> Matrix(CancellationToken ct)
        => Ok(await _matrix.GetMatrix(ct));

    /// <summary>Toggle one cell in the matrix (Admin persona only).</summary>
    [HttpPut("matrix")]
    [ProducesResponseType(typeof(PermissionMatrixEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionMatrixEntry>> Toggle(
        [FromBody] UpdatePermissionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.PersonaCode)
         || string.IsNullOrWhiteSpace(req.FeatureCode)
         || string.IsNullOrWhiteSpace(req.PermissionCode))
            return ValidationProblem("PersonaCode, FeatureCode and PermissionCode are required.");
        return Ok(await _matrix.Upsert(req, ct));
    }
}
