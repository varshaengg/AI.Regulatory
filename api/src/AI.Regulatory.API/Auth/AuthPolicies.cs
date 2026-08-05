namespace AI.Regulatory.API.Auth;

/// <summary>
/// Authorization policy names — see docs/API-Design.md §3.
/// Kept as constants so `[Authorize(Policy = AuthPolicies.AdminOnly)]` compiles.
/// </summary>
public static class AuthPolicies
{
    public const string PermissionClaimType = "ara_perm";

    public const string AdminOnly            = "AdminOnly";
    public const string UserManagementRead  = "UserManagementRead";
    public const string UserManagementWrite = "UserManagementWrite";
    public const string UserManagementAdmin = "UserManagementAdmin";
    public const string TemplatesRead       = "TemplatesRead";
    public const string TemplatesAdmin      = "TemplatesAdmin";
    public const string DossierManagementRead  = "DossierManagementRead";
    public const string DossierManagementWrite = "DossierManagementWrite";
    public const string DossierManagementReview = "DossierManagementReview";
    public const string DossierManagementAdmin  = "DossierManagementAdmin";
    public const string RaLeadOrAdmin       = "RaLeadOrAdmin";
    public const string AuthorScope         = "AuthorScope";
    public const string ReviewerScope       = "ReviewerScope";
}
