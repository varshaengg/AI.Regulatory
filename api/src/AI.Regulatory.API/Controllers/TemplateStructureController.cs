using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AI.Regulatory.API.Controllers;

/// <summary>Draft section hierarchy of a template: AI proposes, an admin reviews and approves.</summary>
[ApiController]
[Route("api/v1/templates/{id}")]
[Authorize(Policy = AuthPolicies.TemplatesAdmin)]
[Tags("Templates (M3)")]
[Produces("application/json")]
public sealed class TemplateStructureController : ControllerBase
{
    private const int MaxNodes = 500;

    private readonly TemplatesRepository _templates;
    private readonly TemplateStructureRepository _structures;
    private readonly ITemplateStructureParser _parser;
    private readonly IWebHostEnvironment _env;

    public TemplateStructureController(
        TemplatesRepository templates, TemplateStructureRepository structures,
        ITemplateStructureParser parser, IWebHostEnvironment env)
    {
        _templates = templates;
        _structures = structures;
        _parser = parser;
        _env = env;
    }

    [HttpGet("draft-structure")]
    [ProducesResponseType(typeof(TemplateStructure), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateStructure>> Get(string id, CancellationToken ct)
    {
        if (await _templates.GetAsync(id, ct) is null)
            return NotFoundProblem($"No template '{id}' exists.");
        return await _structures.GetAsync(id, ct) is { } s ? Ok(s) : NotFoundProblem("This template has no parsed structure yet.");
    }

    /// <summary>Reads the stored PDF and proposes a hierarchy. Replaces any existing draft and resets approval.</summary>
    [HttpPost("parse")]
    [ProducesResponseType(typeof(TemplateStructure), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateStructure>> Parse(string id, CancellationToken ct)
    {
        var template = await _templates.GetAsync(id, ct);
        if (template is null)
            return NotFoundProblem($"No template '{id}' exists.");

        var storagePath = await _templates.GetStoragePathAsync(id, ct);
        var path = storagePath is null ? null : Path.Combine(StorageRoot(), storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (path is null || !System.IO.File.Exists(path))
            return NotFoundProblem("The template file is no longer in storage; upload it again.");

        TemplateParseResult result;
        await using (var stream = System.IO.File.OpenRead(path))
            result = await _parser.ParseAsync(template.ModuleId, template.FileName, stream, ct);

        return Ok(await _structures.SaveAsync(id, "Draft", result.Note, CurrentUserName(), result.Nodes, ct));
    }

    /// <summary>Saves the admin's corrections; with approve=true the structure is validated and marked Approved.</summary>
    [HttpPut("draft-structure")]
    [ProducesResponseType(typeof(TemplateStructure), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateStructure>> Save(string id, [FromBody] SaveTemplateStructureRequest request, CancellationToken ct)
    {
        if (await _templates.GetAsync(id, ct) is null)
            return NotFoundProblem($"No template '{id}' exists.");
        if (request?.Nodes is null)
            return Invalid("nodes is required.");

        var error = Validate(request.Nodes, request.Approve);
        if (error is not null)
            return Invalid(error);

        var clean = request.Nodes.Select(n => new TemplateNode(
            n.NodeKey.Trim(), string.IsNullOrWhiteSpace(n.ParentKey) ? null : n.ParentKey.Trim(),
            n.Title.Trim(), string.IsNullOrWhiteSpace(n.Guidance) ? null : n.Guidance.Trim(), n.IsMandatory)).ToArray();
        var existing = await _structures.GetAsync(id, ct);
        return Ok(await _structures.SaveAsync(id, request.Approve ? "Approved" : "Draft", existing?.Note, CurrentUserName(), clean, ct));
    }

    /// <summary>Unique keys, valid parent links without cycles, and (for approval) at least one node.</summary>
    internal static string? Validate(IReadOnlyList<TemplateNode> nodes, bool approve)
    {
        if (nodes.Count > MaxNodes) return $"A template may have at most {MaxNodes} sections.";
        if (approve && nodes.Count == 0) return "Add at least one section before approving.";

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nodes)
        {
            if (string.IsNullOrWhiteSpace(n.NodeKey) || n.NodeKey.Trim().Length > 50) return "Each section needs a key of 1-50 characters.";
            if (string.IsNullOrWhiteSpace(n.Title) || n.Title.Trim().Length > 300) return $"Section '{n.NodeKey}' needs a title of 1-300 characters.";
            if (n.Guidance?.Length > 2000) return $"Guidance for '{n.NodeKey}' must be 2000 characters or fewer.";
            if (!keys.Add(n.NodeKey.Trim())) return $"Section key '{n.NodeKey}' is used more than once.";
        }

        var parents = nodes.ToDictionary(n => n.NodeKey.Trim(), n => string.IsNullOrWhiteSpace(n.ParentKey) ? null : n.ParentKey.Trim(), StringComparer.OrdinalIgnoreCase);
        foreach (var (key, parent) in parents)
        {
            if (parent is not null && !parents.ContainsKey(parent)) return $"Section '{key}' refers to unknown parent '{parent}'.";
            var hops = 0;
            for (var p = parent; p is not null; p = parents[p])
                if (++hops > parents.Count) return $"Section '{key}' is part of a parent cycle.";
        }
        return null;
    }

    private ActionResult Invalid(string detail)
        => Problem(type: ErrorTypes.Validation, title: "Validation failed", statusCode: StatusCodes.Status400BadRequest, detail: detail);

    private ActionResult NotFoundProblem(string detail)
        => Problem(type: ErrorTypes.NotFound, title: "Not found", statusCode: StatusCodes.Status404NotFound, detail: detail);

    private string StorageRoot()
        => Environment.GetEnvironmentVariable("HOME") is { Length: > 0 } home
            ? Path.Combine(home, "data")
            : _env.ContentRootPath;

    private string CurrentUserName()
        => User.FindFirstValue("name")
            ?? User.FindFirstValue("preferred_username")
            ?? User.Identity?.Name
            ?? "unknown";
}
