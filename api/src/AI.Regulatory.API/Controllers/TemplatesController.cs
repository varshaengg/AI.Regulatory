using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AI.Regulatory.API.Controllers;

/// <summary>CTD template catalog — A2/A3 admin screens.</summary>
[ApiController]
[Route("api/v1/templates")]
[Authorize(Policy = AuthPolicies.TemplatesAdmin)]
[Tags("Templates (M3)")]
[Produces("application/json")]
public sealed class TemplatesController : ControllerBase
{
    private readonly TemplatesRepository _repo;
    private readonly IWebHostEnvironment _env;

    public TemplatesController(TemplatesRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Page<CtdTemplate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Page<CtdTemplate>>> List(CancellationToken ct)
    {
        var items = (await _repo.ListAsync(ct))
            .Where(t => string.Equals(t.Scope, "Global", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Ok(new Page<CtdTemplate>(items, new PageInfo(items.Length, null, false)));
    }

    [HttpPost("global/{moduleId}")]
    [Authorize(Policy = AuthPolicies.TemplatesAdmin)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CtdTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CtdTemplate>> UploadGlobal(
        string moduleId,
        [FromForm] string version,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        var validation = await ValidateUploadAsync(moduleId, version, file, ct);
        if (validation is not null) return validation;

        var storagePath = await SaveTemplateAsync("global", moduleId, file, ct);
        var saved = await _repo.UpsertGlobalAsync(moduleId, version, Path.GetFileName(file.FileName), storagePath, CurrentUserName(), ct);
        return Ok(saved);
    }

    [HttpGet("/api/v1/projects/{projectId}/templates")]
    [Authorize(Policy = AuthPolicies.DossierManagementRead)]
    [ProducesResponseType(typeof(IReadOnlyList<CtdTemplateModuleEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CtdTemplateModuleEntry>>> ResolveForProject(string projectId, CancellationToken ct)
        => Ok(await _repo.ResolveForProjectAsync(projectId, ct));

    [HttpPost("/api/v1/projects/{projectId}/templates/{moduleId}")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CtdTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CtdTemplate>> UploadProjectOverride(
        string projectId,
        string moduleId,
        [FromForm] string version,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        var validation = await ValidateUploadAsync(moduleId, version, file, ct);
        if (validation is not null) return validation;

        var storagePath = await SaveTemplateAsync($"projects/{projectId}", moduleId, file, ct);
        var saved = await _repo.UpsertProjectOverrideAsync(projectId, moduleId, version, Path.GetFileName(file.FileName), storagePath, CurrentUserName(), ct);
        if (saved is null)
        {
            return Problem(
                type: ErrorTypes.NotFound,
                title: "Project not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No project '{projectId}' exists.");
        }

        return Ok(saved);
    }

    [HttpDelete("/api/v1/projects/{projectId}/templates/{moduleId}")]
    [Authorize(Policy = AuthPolicies.DossierManagementWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProjectOverride(string projectId, string moduleId, CancellationToken ct)
        => await _repo.DeleteProjectOverrideAsync(projectId, moduleId, ct) ? NoContent() : NotFound();

    private async Task<ActionResult<CtdTemplate>?> ValidateUploadAsync(string moduleId, string version, IFormFile? file, CancellationToken ct)
    {
        if (!CtdModuleCatalog.All.Any(m => string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"moduleId must be one of {string.Join(", ", CtdModuleCatalog.All.Select(m => m.Id))}.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "version is required.");
        }

        if (file is null || file.Length == 0)
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "A CTD template PDF file is required.");
        }

        if (file.Length > 25 * 1024 * 1024)
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Template file must be 25 MB or smaller.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Only .pdf CTD template files are allowed.");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[5];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < header.Length || !"%PDF-"u8.SequenceEqual(header))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Only valid PDF files are allowed.");
        }

        return null;
    }

    private async Task<string> SaveTemplateAsync(string scope, string moduleId, IFormFile file, CancellationToken ct)
    {
        var storageRoot = Environment.GetEnvironmentVariable("HOME") is { Length: > 0 } home
            ? Path.Combine(home, "data")
            : _env.ContentRootPath;
        var relativeDir = Path.Combine("ctd-templates", scope, moduleId.Trim().ToUpperInvariant());
        var fullDir = Path.Combine(storageRoot, relativeDir);
        Directory.CreateDirectory(fullDir);

        var storedName = $"{Guid.NewGuid():N}.pdf";
        var fullPath = Path.Combine(fullDir, storedName);
        await using (var target = System.IO.File.Create(fullPath))
        await using (var source = file.OpenReadStream())
        {
            await source.CopyToAsync(target, ct);
        }

        return Path.Combine(relativeDir, storedName).Replace(Path.DirectorySeparatorChar, '/');
    }

    private string CurrentUserName()
        => User.FindFirstValue("name")
            ?? User.FindFirstValue("preferred_username")
            ?? User.Identity?.Name
            ?? "unknown";
}
