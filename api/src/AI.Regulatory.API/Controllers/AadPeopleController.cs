using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace AI.Regulatory.API.Controllers;

/// <summary>
/// Customer AD people-picker proxy. The SPA calls this from the A5 add-user
/// flow to search only the customer tenant — never the wider directory graph.
/// </summary>
/// <remarks>
/// Backing store:
/// <list type="bullet">
///   <item><description>Mocked mode (<c>Data:IsMocked=true</c>): reads from <see cref="AadPeopleRepository"/>.</description></item>
///   <item><description>Live mode: calls Microsoft Graph via On-Behalf-Of flow — the caller's user token
///     is exchanged for a Graph token with delegated scope <c>User.ReadBasic.All</c>. This means
///     results are automatically scoped to the caller's home tenant.</description></item>
/// </list>
/// Live mode requires the API's Entra app to hold delegated <c>User.ReadBasic.All</c> on
/// Microsoft Graph, granted with admin consent.
/// </remarks>
[ApiController]
[Route("api/v1/aad")]
[Authorize]
[Tags("People (A5)")]
[Produces("application/json")]
public sealed class AadPeopleController : ControllerBase
{
    private readonly AadPeopleRepository _repo;
    private readonly GraphServiceClient? _graph;
    private readonly bool _isMocked;

    public AadPeopleController(
        AadPeopleRepository repo,
        IOptions<Data.DataOptions> dataOptions,
        GraphServiceClient? graph = null)
    {
        _repo = repo;
        _graph = graph;
        _isMocked = dataOptions.Value.IsMocked;
    }

    /// <summary>Search by display name or email prefix. Returns up to <paramref name="top"/> hits (max 25).</summary>
    [HttpGet("people")]
    [ProducesResponseType(typeof(IReadOnlyList<AadPerson>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AadPerson>>> Search(
        [FromQuery] string search, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        search = (search ?? string.Empty).Trim();
        if (search.Length < 3) return Ok(Array.Empty<AadPerson>());

        // Mocked mode — return canned people.
        if (_isMocked || _graph is null)
            return Ok(await _repo.Search(search, top, ct));

        // Live mode — call Graph via OBO. $search requires ConsistencyLevel:eventual.
        var pageSize = Math.Clamp(top, 1, 25);
        var quoted = search.Replace("\"", string.Empty);
        var searchExpr = $"\"displayName:{quoted}\" OR \"mail:{quoted}\"";

        var users = await _graph.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Search = searchExpr;
            cfg.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle" };
            cfg.QueryParameters.Top    = pageSize;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        }, ct);

        var results = (users?.Value ?? new List<Microsoft.Graph.Models.User>())
            .Select(u => new AadPerson(
                AadObjectId: u.Id ?? string.Empty,
                DisplayName: u.DisplayName ?? "(no name)",
                Email:       u.Mail ?? u.UserPrincipalName ?? string.Empty,
                JobTitle:    u.JobTitle ?? string.Empty))
            .ToArray();

        return Ok(results);
    }
}
