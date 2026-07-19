namespace AI.Regulatory.API.Auth;

/// <summary>
/// Authorization policy names — see docs/API-Design.md §3.
/// Kept as constants so `[Authorize(Policy = AuthPolicies.AdminOnly)]` compiles.
/// </summary>
public static class AuthPolicies
{
    public const string AdminOnly      = "AdminOnly";
    public const string RaLeadOrAdmin  = "RaLeadOrAdmin";
    public const string AuthorScope    = "AuthorScope";
    public const string ReviewerScope  = "ReviewerScope";
}
