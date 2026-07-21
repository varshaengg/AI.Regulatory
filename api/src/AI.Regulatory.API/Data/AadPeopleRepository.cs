using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>
/// People-picker source. In mocked mode returns a canned list of AAD-shaped
/// hits so the A5 add-user flow works end-to-end without a Graph call. In
/// live mode this will be replaced by a Microsoft Graph proxy that filters
/// to the customer tenant only (see SDD §4.1).
/// </summary>
public sealed class AadPeopleRepository : BaseRepository<AadPerson>
{
    public AadPeopleRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(AadPerson item, string id)
        => string.Equals(item.AadObjectId, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<AadPerson> SeedData() => new[]
    {
        new AadPerson("aad-sara-o",   "Sara O'Neill",     "sara.oneill@contoso.com",  "IT Administrator"),
        new AadPerson("aad-marcus-h", "Marcus Hollander", "marcus.h@contoso.com",     "Regulatory Lead"),
        new AadPerson("aad-priya-r",  "Priya Ramanathan", "priya.r@contoso.com",      "Regulatory Author"),
        new AadPerson("aad-chen-l",   "Dr. Chen Liu",     "chen.liu@contoso.com",     "Regulatory Reviewer"),
        new AadPerson("aad-mika-t",   "Mika Tanaka",      "mika.t@contoso.com",       "Sr. Regulatory"),
        new AadPerson("aad-alex-w",   "Alex Wang",        "alex.wang@contoso.com",    "QA Manager"),
        new AadPerson("aad-nina-p",   "Nina Petrov",      "nina.p@contoso.com",       "Clinical Lead"),
        new AadPerson("aad-diego-c",  "Diego Costa",      "diego.c@contoso.com",      "Data Steward"),
        new AadPerson("aad-yumi-k",   "Yumi Kato",        "yumi.k@contoso.com",       "Regulatory Author"),
        new AadPerson("aad-omar-s",   "Omar Saleh",       "omar.s@contoso.com",       "Regulatory Reviewer"),
    };

    /// <summary>
    /// Substring search across displayName + email. Case-insensitive.
    /// Live implementation will call
    /// <c>GET https://graph.microsoft.com/v1.0/users?$search="displayName:{q}" OR "mail:{q}"</c>
    /// with the API's managed identity + <c>User.Read.All</c> app permission.
    /// </summary>
    public async Task<IReadOnlyList<AadPerson>> Search(string query, int top, CancellationToken ct)
    {
        var all = await ListAsync(ct);
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<AadPerson>();
        var q = query.Trim();
        return all
            .Where(p => p.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || p.Email.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(Math.Clamp(top, 1, 25))
            .ToArray();
    }
}
