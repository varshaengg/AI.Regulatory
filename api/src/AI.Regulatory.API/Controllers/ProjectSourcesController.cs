using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Regulatory.API.Controllers;

/// <summary>Per-project source configuration — A4.</summary>
[ApiController]
[Route("api/v1/projects/{projectId}/sources")]
[Authorize]
[Tags("Project sources")]
[Produces("application/json")]
public sealed class ProjectSourcesController : ControllerBase
{
    private readonly ProjectSourcesRepository _repo;
    public ProjectSourcesController(ProjectSourcesRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSourcesByModule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSourcesByModule>>> ByProject(string projectId, CancellationToken ct)
        => Ok(await _repo.ByProjectAsync(projectId, ct));
}

/// <summary>CTD module catalog + sub-module coverage — L4.</summary>
[ApiController]
[Route("api/v1/modules")]
[Authorize]
[Tags("CTD modules")]
[Produces("application/json")]
public sealed class ModulesController : ControllerBase
{
    private readonly SubModulesRepository _submodules;
    public ModulesController(SubModulesRepository submodules) => _submodules = submodules;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CtdModule>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CtdModule>> ListModules() => Ok(CtdModuleCatalog.All);

    [HttpGet("{moduleId}/submodules")]
    [ProducesResponseType(typeof(IReadOnlyList<SubModule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubModule>>> ListSubModules(string moduleId, CancellationToken ct)
    {
        // Filter seeded sub-modules by module prefix (3.2.S / 3.2.P belong to M3).
        var all = await _submodules.ListAsync(ct);
        var filtered = moduleId.Equals("M3", StringComparison.OrdinalIgnoreCase)
            ? all.Where(s => s.Code.StartsWith("3.2.")).ToArray()
            : (IReadOnlyList<SubModule>)Array.Empty<SubModule>();
        return Ok(filtered);
    }
}
