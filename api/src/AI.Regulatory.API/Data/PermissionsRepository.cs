using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Permission verbs — the "Z" axis of a persona × feature cell.</summary>
/// <remarks>
/// Verbs are ordered Read &lt; Write &lt; Review &lt; Admin. Admin implies all
/// lower verbs when the effective permission is computed — see
/// <see cref="PermissionMatrixRepository.GetMatrix"/>.
/// </remarks>
public sealed class PermissionsRepository : BaseRepository<Permission>
{
    public PermissionsRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(Permission item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<Permission> SeedData() => new[]
    {
        new Permission("Read",   "Read",   10),
        new Permission("Write",  "Write",  20),
        new Permission("Review", "Review", 30),
        new Permission("Admin",  "Admin",  40),
    };
}
