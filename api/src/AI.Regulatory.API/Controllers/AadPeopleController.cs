using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly AppUsersRepository? _users;

    public AadPeopleController(
        AadPeopleRepository repo,
        AppUsersRepository? users = null,
        GraphServiceClient? graph = null)
    {
        _repo = repo;
        _users = users;
        _graph = graph;
    }

    /// <summary>Search by display name or email prefix. Returns up to <paramref name="top"/> hits (max 25).</summary>
    [HttpGet("people")]
    [ProducesResponseType(typeof(IReadOnlyList<AadPerson>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AadPerson>>> Search(
        [FromQuery] string search, [FromQuery] int top = 10, CancellationToken ct = default)
    {
        search = (search ?? string.Empty).Trim();
        if (search.Length < 3) return Ok(Array.Empty<AadPerson>());

        // Fall back to the in-memory seed only when there's no Graph client at
        // all (no EntraId config — happens in local dev / smoke deploys).
        if (_graph is null)
            return Ok(await _repo.Search(search, top, ct));

        // Live mode — call Graph via OBO. $search requires ConsistencyLevel:eventual.
        var pageSize = Math.Clamp(top, 1, 25);
        var quoted = search.Replace("\"", string.Empty);
        var searchExpr = $"\"displayName:{quoted}\" OR \"mail:{quoted}\"";

        try
        {
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
        catch (Exception ex)
        {
            // Fall back to seed repo when Graph call fails (e.g., OBO failure).
            // Also attempt to resolve from already-enrolled AppUser rows if available.
            try
            {
                var seed = await _repo.Search(search, top, ct);
                if (seed.Any()) return Ok(seed);

                if (_users is not null)
                {
                    var all = await _users.ListAsync(ct);
                    var matches = all
                        .Where                        (x => (x.DisplayName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                                                         || (x.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                        .Take(pageSize)
                        .Select(u => new AadPerson(u.AadObjectId, u.DisplayName, u.Email, u.JobTitle ?? string.Empty))
                        .ToArray();
                    if (matches.Any()) return Ok(matches);
                }
            }
            catch { /* swallow secondary errors and continue to empty result */ }

            return Ok(Array.Empty<AadPerson>());
        }
    }

    /// <summary>
    /// Try to resolve a single user by email (fallback path used when people-picker
    /// input is an exact email and the regular $search returned no hits). This helps
    /// the SPA accept typed emails and also enables "load from DB" fallback when
    /// Graph isn't available.
    /// </summary>
    [HttpGet("people/resolve")]
    [ProducesResponseType(typeof(AadPerson), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AadPerson>> Resolve([FromQuery] string email, CancellationToken ct = default)
    {
        var q = (email ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(q)) return BadRequest();

        // Try Graph first (live mode)
        if (_graph is not null)
        {
            var filter = $"mail eq '{q}' or userPrincipalName eq '{q}'";
            var users = await _graph.Users.GetAsync(cfg =>
            {
                cfg.QueryParameters.Filter = filter;
                cfg.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle" };
                cfg.QueryParameters.Top = 1;
            }, ct);

            var u = users?.Value?.FirstOrDefault();
            if (u is not null)
            {
                return Ok(new AadPerson(
                    AadObjectId: u.Id ?? string.Empty,
                    DisplayName: u.DisplayName ?? "(no name)",
                    Email:       u.Mail ?? u.UserPrincipalName ?? string.Empty,
                    JobTitle:    u.JobTitle ?? string.Empty));
            }
        }

        // Fallback to seed repository (mocked mode)
        var seedHits = await _repo.Search(q, 1, ct);
        if (seedHits.Any()) return Ok(seedHits.First());

        // Finally try existing AppUser rows in SQL (helps when user already enrolled)
        if (_users is not null)
        {
            var all = await _users.ListAsync(ct);
            var match = all.FirstOrDefault(x => string.Equals(x.Email, q, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return Ok(new AadPerson(
                                AadObjectId: match.AadObjectId,
                                DisplayName: match.DisplayName,
                                Email:       match.Email,
                                JobTitle:    match.JobTitle ?? string.Empty));
            }
        }

        return NotFound();
    }
}
