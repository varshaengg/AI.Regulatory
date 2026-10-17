using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AI.Regulatory.API.Controllers;

/// <summary>Module guideline catalog — admin-managed validation rules the AI applies to discovered documents (advisory).</summary>
[ApiController]
[Route("api/v1/guidelines")]
[Authorize(Policy = AuthPolicies.TemplatesAdmin)]
[Tags("Module guidelines")]
[Produces("application/json")]
public sealed partial class GuidelinesController : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".pdf", ".docx"];

    private readonly GuidelinesRepository _repo;
    private readonly IGuidelineRuleExtractor _extractor;
    private readonly IWebHostEnvironment _env;

    public GuidelinesController(GuidelinesRepository repo, IGuidelineRuleExtractor extractor, IWebHostEnvironment env)
    {
        _repo = repo;
        _extractor = extractor;
        _env = env;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Page<ModuleGuideline>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Page<ModuleGuideline>>> List(CancellationToken ct)
    {
        var items = await _repo.ListAsync(ct);
        return Ok(new Page<ModuleGuideline>(items, new PageInfo(items.Count, null, false)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ModuleGuideline), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleGuideline>> Get(string id, CancellationToken ct)
        => await _repo.GetAsync(id, ct) is { } g ? Ok(g) : NotFoundProblem(id);

    /// <summary>Uploads a guideline document; AI proposes draft rules that stay unpublished until an admin reviews them.</summary>
    [HttpPost("{moduleId}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ModuleGuideline), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModuleGuideline>> Upload(
        string moduleId,
        [FromForm] string version,
        IFormFile file,
        CancellationToken ct,
        [FromForm] string? country = null)
    {
        moduleId = moduleId.Trim().ToUpperInvariant();
        if (!CtdModuleCatalog.All.Any(m => string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase)))
            return Invalid($"moduleId must be one of {string.Join(", ", CtdModuleCatalog.All.Select(m => m.Id))}.");
        if (string.IsNullOrWhiteSpace(version))
            return Invalid("version is required.");
        if (file is null || file.Length == 0)
            return Invalid("A guideline file is required.");
        if (file.Length > 25 * 1024 * 1024)
            return Invalid("Guideline file must be 25 MB or smaller.");
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return Invalid("Only .pdf and .docx guideline files are allowed.");

        var normalizedCountry = TemplatesRepository.NormalizeCountry(country);
        if (normalizedCountry != TemplatesRepository.GlobalCountry && !CountryCode().IsMatch(normalizedCountry))
            return Invalid("country must be \"Global\" or a two-letter country code.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storagePath = await SaveAsync(normalizedCountry, moduleId, extension, file, ct);

        GuidelineExtraction extraction;
        if (extension == ".pdf")
        {
            await using var stream = file.OpenReadStream();
            extraction = await _extractor.ExtractAsync(moduleId, file.FileName, stream, ct);
        }
        else
        {
            extraction = new GuidelineExtraction([], "DOCX rule extraction is not supported yet; add rules manually.");
        }

        var saved = await _repo.CreateAsync(moduleId, normalizedCountry, version.Trim(), Path.GetFileName(file.FileName),
            storagePath, CurrentUserName(), extraction.Note, extraction.Rules, ct);
        return Ok(saved);
    }

    /// <summary>Replaces the reviewed rule list.</summary>
    [HttpPut("{id}/rules")]
    [ProducesResponseType(typeof(ModuleGuideline), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleGuideline>> SaveRules(string id, [FromBody] SaveGuidelineRulesRequest request, CancellationToken ct)
    {
        if (request?.Rules is null || request.Rules.Count > 200)
            return Invalid("rules is required and may contain at most 200 entries.");

        var clean = new List<GuidelineRule>();
        foreach (var r in request.Rules)
        {
            if (string.IsNullOrWhiteSpace(r.Text) || r.Text.Length > 2000)
                return Invalid("Each rule needs text of 1-2000 characters.");
            var severity = GuidelinesRepository.Severities.FirstOrDefault(s => string.Equals(s, r.Severity, StringComparison.OrdinalIgnoreCase));
            if (severity is null)
                return Invalid("severity must be Low, Medium or High.");
            if (r.NodeRef?.Length > 100)
                return Invalid("nodeRef must be 100 characters or fewer.");
            clean.Add(new GuidelineRule($"R{clean.Count + 1:D3}", r.Text.Trim(), severity, string.IsNullOrWhiteSpace(r.NodeRef) ? null : r.NodeRef.Trim()));
        }

        return await _repo.SaveRulesAsync(id, clean, ct) is { } g ? Ok(g) : NotFoundProblem(id);
    }

    /// <summary>Publishes the guideline; it replaces any other published guideline for the same country and module.</summary>
    [HttpPut("{id}/publish")]
    [ProducesResponseType(typeof(ModuleGuideline), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleGuideline>> Publish(string id, CancellationToken ct)
    {
        var existing = await _repo.GetAsync(id, ct);
        if (existing is null) return NotFoundProblem(id);
        if (existing.Rules.Count == 0)
            return Invalid("Add at least one rule before publishing.");
        return await _repo.PublishAsync(id, ct) is { } g ? Ok(g) : NotFoundProblem(id);
    }

    [HttpPut("{id}/unpublish")]
    [ProducesResponseType(typeof(ModuleGuideline), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleGuideline>> Unpublish(string id, CancellationToken ct)
        => await _repo.UnpublishAsync(id, ct) is { } g ? Ok(g) : NotFoundProblem(id);

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
        => await _repo.DeleteAsync(id, ct) ? NoContent() : NotFoundProblem(id).Result!;

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryCode();

    private ActionResult Invalid(string detail)
        => Problem(type: ErrorTypes.Validation, title: "Validation failed", statusCode: StatusCodes.Status400BadRequest, detail: detail);

    private ActionResult<ModuleGuideline> NotFoundProblem(string id)
        => Problem(type: ErrorTypes.NotFound, title: "Guideline not found", statusCode: StatusCodes.Status404NotFound,
            detail: $"No active guideline '{id}' exists.");

    private async Task<string> SaveAsync(string country, string moduleId, string extension, IFormFile file, CancellationToken ct)
    {
        var storageRoot = Environment.GetEnvironmentVariable("HOME") is { Length: > 0 } home
            ? Path.Combine(home, "data")
            : _env.ContentRootPath;
        var relativeDir = Path.Combine("guidelines", country, moduleId);
        var fullDir = Path.Combine(storageRoot, relativeDir);
        Directory.CreateDirectory(fullDir);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        await using (var target = System.IO.File.Create(Path.Combine(fullDir, storedName)))
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
