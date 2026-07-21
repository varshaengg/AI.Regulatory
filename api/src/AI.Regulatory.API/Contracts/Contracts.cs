namespace AI.Regulatory.API.Contracts;

// ─── Meta / bootstrap ─────────────────────────────────────────────────────────

/// <summary>SDD §3.4 — SPA bootstrap config returned by GET /bff/config.</summary>
public sealed record BffConfig(
    string TenantId,
    string ApiBaseUrl,
    IReadOnlyDictionary<string, bool> FeatureFlags,
    BrandingTokens Branding);

public sealed record BrandingTokens(
    string ProductName,
    string PrimaryColor,
    string? LogoUrl);

/// <summary>Current user profile — GET /bff/user, GET /api/v1/me.</summary>
public sealed record UserProfile(
    string Id,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Roles,
    string? AvatarUrl);

/// <summary>Cursor-paginated collection envelope — API-Design §6.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, PageInfo PageInfo);
public sealed record PageInfo(int PageSize, string? NextCursor, bool HasMore);


// ─── Projects — L1, L2, L5, L6, R1 ────────────────────────────────────────────

/// <summary>
/// Project row for list screens (L1, L2). Includes L2's extended fields —
/// product code, modules, progress %, owner display name.
/// </summary>
public sealed record ProjectSummary(
    string Id,
    string Name,
    string Country,
    string Status,
    string Product,
    IReadOnlyList<string> Modules,
    string OwnerDisplayName,
    int ProgressPct,
    DateTime CreatedAt);

public sealed record ProjectDetail(
    string Id,
    string Name,
    string Country,
    string Status,
    string Product,
    IReadOnlyList<string> Modules,
    string OwnerEmail,
    string OwnerDisplayName,
    int ProgressPct,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Etag);

public sealed record CreateProjectRequest(
    string Name,
    string Country,
    string? Product);


// ─── Templates catalog — A2, A3 ───────────────────────────────────────────────

public sealed record CtdTemplate(
    string Id,
    string Country,
    string Region,
    IReadOnlyList<string> Modules,
    string Version,
    string UploadedBy,
    DateTime UploadedOn,
    string Status);       // Active | Draft | Archived


// ─── Project sources — A4, L5 ─────────────────────────────────────────────────

public sealed record ProjectSource(
    int Id,
    string ProjectId,
    string ModuleId,
    string Label,
    string Path,
    string Type,          // Azure Blob | SharePoint
    DateTime SyncedAt,
    string Status);       // ok | warning | error

public sealed record ProjectSourcesByModule(
    string ModuleId,
    string Label,
    string Color,
    IReadOnlyList<ProjectSource> Sources);


// ─── Modules & sub-modules — L4, L5 ───────────────────────────────────────────

public sealed record CtdModule(
    string Id,
    string Label,
    string Color,
    string Status);       // Configured | Needs input

public sealed record SubModule(
    string Code,
    string Title,
    bool Included,
    string SourceStatus,  // Found | Partial | Missing
    string? Notes);


// ─── Author assignments — U1 ──────────────────────────────────────────────────

public sealed record Assignment(
    string SectionCode,
    string Title,
    string ProjectContext,
    string Confidence,     // Low | Med | High
    int CitationsCount,
    bool Flagged);


// ─── Reviewer comments — R1 ───────────────────────────────────────────────────

public sealed record Comment(
    int Id,
    string SectionCode,
    string Author,
    string Initials,
    DateTime CreatedAt,
    string Text,
    bool Resolved);


// ─── Dossier runs — L6 ────────────────────────────────────────────────────────

public sealed record DossierRun(
    string Id,
    string ProjectId,
    string Status,        // running | complete | blocked
    DateTime StartedAt,
    IReadOnlyList<RunModule> Modules,
    IReadOnlyList<RunLogEntry> Log,
    IReadOnlyList<RunAgent> Agents);

public sealed record RunModule(
    string Id,
    string Label,
    int Done,
    int Total,
    int Pct,
    string Activity,
    string Status);       // ok | running | blocked

public sealed record RunLogEntry(
    string Time,          // HH:mm:ss for the wireframe
    string Level,         // INFO | OK | WARN | ERROR
    string Message);

public sealed record RunAgent(
    string Name,
    string Status);       // running | idle


// ─── Notifications — L1 ───────────────────────────────────────────────────────

public sealed record UserNotification(
    string Id,
    string Kind,          // success | warning | error | info | mention
    string Text,
    DateTime CreatedAt);


// ─── Document tree — R1 ───────────────────────────────────────────────────────

public sealed record DocTreeNode(
    string Code,
    string Label,
    string? ModuleId,
    string? Color,
    int Comments,
    bool Active,
    IReadOnlyList<DocTreeNode> Children);


// ─── Admin — A1 ───────────────────────────────────────────────────────────────

public sealed record AdminStats(
    int TemplateCount,
    int CountriesMapped,
    int StorageAccounts,
    int PendingSetupTasks);

public sealed record AdminActivity(
    string Id,
    string Tag,
    string Description,
    DateTime OccurredAt);


// ─── User & Access Management — A5, A6 ────────────────────────────────────────
// Persona × Feature × Permission model. Mirrors the SQL schema in
// data/sql/AI.Regulatory.Sql. Read-time joins produce PermissionMatrixEntry
// and AppUser rows for the UI.

/// <summary>Role bucket (Admin, RaLead, RaAuthor, RaReviewer + tenant custom).</summary>
public sealed record Persona(
    string Code,           // stable machine name — 'Admin', 'RaLead', ...
    string Name,           // display name
    string? Description,
    bool IsSystem);        // system personas cannot be renamed/deleted

/// <summary>Application feature protected by permissions (UserManagement, DossierManagement, ...).</summary>
public sealed record Feature(
    string Code,
    string Name,
    string Category,       // Administration | Regulatory | Platform
    int SortOrder);

/// <summary>Permission verb (Read / Write / Review / Admin) — Admin implies all lower verbs.</summary>
public sealed record Permission(
    string Code,           // 'Read' | 'Write' | 'Review' | 'Admin'
    string Name,
    int SortOrder);

/// <summary>One row of the persona × feature × permission matrix (Admin scope: A6).</summary>
public sealed record PermissionMatrixEntry(
    string PersonaCode,
    string FeatureCode,
    string PermissionCode,
    bool Granted);

/// <summary>Bulk PUT body used by A6 to toggle a single cell in the matrix.</summary>
public sealed record UpdatePermissionRequest(
    string PersonaCode,
    string FeatureCode,
    string PermissionCode,
    bool Granted);

/// <summary>A person the customer has enrolled in ARA. Not every AAD user is an AppUser.</summary>
public sealed record AppUser(
    string Id,             // internal guid (matches AppUser.Id in SQL)
    string AadObjectId,    // Entra ID objectId (unique per tenant)
    string DisplayName,
    string Email,
    string? JobTitle,
    IReadOnlyList<string> PersonaCodes,   // one user can hold multiple personas
    DateTime AddedAt,
    string AddedBy);

/// <summary>Request to enrol a new AppUser — comes from A5 add flow after people picker.</summary>
public sealed record CreateAppUserRequest(
    string AadObjectId,
    string DisplayName,
    string Email,
    string? JobTitle,
    IReadOnlyList<string> PersonaCodes);

/// <summary>Add or replace personas on an existing user (A5 edit dialog).</summary>
public sealed record AssignPersonasRequest(
    IReadOnlyList<string> PersonaCodes);

/// <summary>People-picker hit — comes from Graph proxy /api/v1/aad/people?search=.</summary>
public sealed record AadPerson(
    string AadObjectId,
    string DisplayName,
    string Email,
    string? JobTitle);

/// <summary>Effective permissions for the caller — used by UI gating (GET /api/v1/me/permissions).</summary>
public sealed record MePermissions(
    IReadOnlyList<string> Personas,
    IReadOnlyList<PermissionGrant> Grants);

public sealed record PermissionGrant(
    string FeatureCode,
    IReadOnlyList<string> Permissions);   // e.g. ["Read","Write","Review"]
