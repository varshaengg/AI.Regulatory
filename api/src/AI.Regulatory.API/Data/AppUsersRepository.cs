using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>
/// App users + their persona assignments — A5. Seed contains a handful of
/// representative accounts spanning every persona so the mocked A5 grid is
/// meaningful on first load.
/// </summary>
public sealed class AppUsersRepository : BaseRepository<AppUser>
{
    public AppUsersRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(AppUser item, string id)
        => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)
        || string.Equals(item.AadObjectId, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<AppUser> SeedData() => new[]
    {
        new AppUser("u-sara",   "aad-sara-o",   "Sara O'Neill",   "sara.oneill@contoso.com",  "IT Administrator",   ["Admin"],                DateTime.UtcNow.AddDays(-120), "system"),
        new AppUser("u-marcus", "aad-marcus-h", "Marcus Hollander","marcus.h@contoso.com",    "Regulatory Lead",    ["RaLead"],               DateTime.UtcNow.AddDays(-95),  "sara.oneill@contoso.com"),
        new AppUser("u-priya",  "aad-priya-r",  "Priya Ramanathan","priya.r@contoso.com",     "Regulatory Author",  ["RaAuthor"],             DateTime.UtcNow.AddDays(-60),  "marcus.h@contoso.com"),
        new AppUser("u-chen",   "aad-chen-l",   "Dr. Chen Liu",    "chen.liu@contoso.com",    "Regulatory Reviewer",["RaReviewer"],           DateTime.UtcNow.AddDays(-40),  "marcus.h@contoso.com"),
        new AppUser("u-mika",   "aad-mika-t",   "Mika Tanaka",     "mika.t@contoso.com",      "Sr. Regulatory",     ["RaLead", "RaReviewer"], DateTime.UtcNow.AddDays(-10),  "sara.oneill@contoso.com"),
    };

    /// <summary>Enrol a person previously chosen via the people picker.</summary>
    public async Task<AppUser> Create(CreateAppUserRequest req, string addedBy, CancellationToken ct)
    {
        // Idempotency: if the AAD objectId already exists, just merge personas.
        var existing = await GetAsync(req.AadObjectId, ct);
        if (existing is not null)
        {
            var merged = existing with
            {
                PersonaCodes = existing.PersonaCodes.Union(req.PersonaCodes, StringComparer.OrdinalIgnoreCase).ToArray(),
            };
            return await Replace(merged, ct);
        }

        var user = new AppUser(
            Id:            $"u-{Guid.NewGuid():n}"[..10],
            AadObjectId:   req.AadObjectId,
            DisplayName:   req.DisplayName,
            Email:         req.Email,
            JobTitle:      req.JobTitle,
            PersonaCodes:  req.PersonaCodes,
            AddedAt:       DateTime.UtcNow,
            AddedBy:       addedBy);

        return await AddAsync(user, ct);
    }

    /// <summary>Replace persona list on an existing user (A5 edit dialog).</summary>
    public async Task<AppUser?> AssignPersonas(string userId, AssignPersonasRequest req, CancellationToken ct)
    {
        var existing = await GetAsync(userId, ct);
        if (existing is null) return null;
        return await Replace(existing with { PersonaCodes = req.PersonaCodes }, ct);
    }

    /// <summary>Remove a user entirely.</summary>
    public async Task<bool> Delete(string userId, CancellationToken ct)
    {
        var existing = await GetAsync(userId, ct);
        if (existing is null) return false;
        if (IsMocked)
        {
            SeedList.RemoveAll(u => u.Id.Equals(existing.Id, StringComparison.OrdinalIgnoreCase));
            return true;
        }
        return await DeleteFromStoreAsync(userId, ct);
    }

    private async Task<AppUser> Replace(AppUser user, CancellationToken ct)
    {
        if (IsMocked)
        {
            SeedList.RemoveAll(u => u.Id.Equals(user.Id, StringComparison.OrdinalIgnoreCase));
            SeedList.Add(user);
            return user;
        }
        return await AddToStoreAsync(user, ct);
    }

    private Task<bool> DeleteFromStoreAsync(string id, CancellationToken ct)
        => throw new NotImplementedException("Live store delete not yet implemented for AppUsersRepository.");
}
