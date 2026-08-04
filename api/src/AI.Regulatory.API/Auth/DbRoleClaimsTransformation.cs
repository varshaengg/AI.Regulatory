using System.Security.Claims;
using AI.Regulatory.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace AI.Regulatory.API.Auth;

/// <summary>
/// Enriches the authenticated principal with role claims sourced from AppUsers
/// persona assignments so API policies can be enforced from DB-managed access.
/// </summary>
public sealed class DbRoleClaimsTransformation(
    AppUsersRepository users,
    PermissionMatrixRepository matrix,
    IConfiguration config) : IClaimsTransformation
{
    private static readonly IReadOnlyDictionary<string, string> PersonaToRole =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = "admin",
            ["RaLead"] = "raLead",
            ["RaAuthor"] = "raAuthor",
            ["RaReviewer"] = "raReviewer",
        };

    /// <inheritdoc />
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        var oid = ResolveOid(principal);
        if (string.IsNullOrWhiteSpace(oid))
            return principal;

        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bootstrap = config.GetSection("Admin:BootstrapOids").Get<string[]>() ?? Array.Empty<string>();
        if (bootstrap.Any(x => string.Equals(x, oid, StringComparison.OrdinalIgnoreCase)))
            roles.Add("admin");

        var appUser = await users.GetAsync(oid, CancellationToken.None);
        if (appUser is not null)
        {
            foreach (var persona in appUser.PersonaCodes)
            {
                if (PersonaToRole.TryGetValue(persona, out var role))
                    roles.Add(role);
            }

            var grants = await matrix.GetEffectivePermissions(appUser.PersonaCodes, CancellationToken.None);
            foreach (var grant in grants)
            {
                foreach (var verb in grant.Permissions)
                {
                    var value = $"{grant.FeatureCode}:{verb}";
                    if (!identity.HasClaim(AuthPolicies.PermissionClaimType, value))
                        identity.AddClaim(new Claim(AuthPolicies.PermissionClaimType, value));
                }
            }
        }

        foreach (var role in roles)
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        return principal;
    }

    private static string? ResolveOid(ClaimsPrincipal user) =>
        user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? user.FindFirstValue("oid")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
}
