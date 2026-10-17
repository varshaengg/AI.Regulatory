using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AI.Regulatory.API.Controllers;

/// <summary>CTD template catalog — A2/A3 admin screens.</summary>
[ApiController]
[Route("api/v1/templates")]
[Authorize(Policy = AuthPolicies.TemplatesAdmin)]
[Tags("Templates (M3)")]
[Produces("application/json")]
public sealed partial class TemplatesController : ControllerBase
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
        IFormFile file,
        CancellationToken ct,
        [FromForm] string? country = null,
        [FromForm] bool makeDefault = false,
        [FromForm] bool publish = true)
    {
        var validation = await ValidateUploadAsync(moduleId, version, file, ct);
        if (validation is not null) return validation;

        var normalizedCountry = TemplatesRepository.NormalizeCountry(country);
        if (normalizedCountry != TemplatesRepository.GlobalCountry && !CountryCode().IsMatch(normalizedCountry))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: "country must be \"Global\" or a two-letter country code.");
        }

        var storagePath = await SaveTemplateAsync($"global/{normalizedCountry}", moduleId, file, ct);
        var saved = await _repo.UpsertGlobalAsync(moduleId, normalizedCountry, version, Path.GetFileName(file.FileName), storagePath, CurrentUserName(), makeDefault, publish, ct);
        return Ok(saved);
    }

    /// <summary>Marks a global template as the default for its country and module; other templates for the same pair stop being the default.</summary>
    [HttpPut("{id}/default")]
    [Authorize(Policy = AuthPolicies.TemplatesAdmin)]
    [ProducesResponseType(typeof(CtdTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CtdTemplate>> SetDefault(string id, CancellationToken ct)
    {
        var saved = await _repo.SetDefaultAsync(id, ct);
        if (saved is not null)
            return Ok(saved);

        return Problem(
            type: ErrorTypes.NotFound,
            title: "Template not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: $"No published global CTD template '{id}' exists. Publish it before setting it as default.");
    }

    /// <summary>Publishes an in-review global template so it can be used for dossiers.</summary>
    [HttpPut("{id}/publish")]
    [Authorize(Policy = AuthPolicies.TemplatesAdmin)]
    [ProducesResponseType(typeof(CtdTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CtdTemplate>> Publish(string id, CancellationToken ct)
    {
        var saved = await _repo.PublishAsync(id, ct);
        if (saved is not null)
            return Ok(saved);

        return Problem(
            type: ErrorTypes.NotFound,
            title: "Template not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: $"No active global CTD template '{id}' exists.");
    }

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryCode();

    /// <summary>Archives a global CTD template; projects without an override lose their default for that module.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicies.TemplatesAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGlobal(string id, CancellationToken ct)
    {
        if (await _repo.DeleteGlobalAsync(id, ct))
            return NoContent();

        return Problem(
            type: ErrorTypes.NotFound,
            title: "Template not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: $"No active global CTD template '{id}' exists.");
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
        IFormFile file,
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
        if (!string.Equals(moduleId, TemplatesRepository.AllModules, StringComparison.OrdinalIgnoreCase)
            && !CtdModuleCatalog.All.Any(m => string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase)))
        {
            return Problem(
                type: ErrorTypes.Validation,
                title: "Validation failed",
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"moduleId must be ALL or one of {string.Join(", ", CtdModuleCatalog.All.Select(m => m.Id))}.");
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
