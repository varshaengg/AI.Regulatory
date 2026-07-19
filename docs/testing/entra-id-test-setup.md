# Entra ID Test Setup — Microsoft 365 Developer Tenants (Multi-Tenant Ready)

> **Purpose**: Stand up two free Microsoft 365 Developer tenants so you can end-to-end test the AI Regulatory Assistant (ARA) with realistic Entra ID sign-in, RBAC via security groups, SharePoint Online (SPO) delegated access, **and** exercise the multi-tenant SaaS capability.
>
> **Time**: ~60–90 minutes for the full setup. ~15 minutes if you only need the primary tenant.
>
> **Cost**: Zero. Free Microsoft 365 E5 developer sandbox, renewable every 90 days as long as you use it.

---

## 1. Target Topology

| Role | Tenant | Purpose | Test users |
|---|---|---|---|
| **Publisher tenant** ("home") | `stara-dev.onmicrosoft.com` | Where the ARA app is registered and where the primary dev pilots run | 4 test users covering all RBAC roles |
| **Customer tenant** ("guest") | `stara-cust1.onmicrosoft.com` | Second tenant used to prove multi-tenant sign-in, admin consent flow, and tenant data isolation | 2 test users |

You can add a third tenant later (`stara-cust2`) if you need to test cross-tenant edge cases (e.g., group-name collisions).

---

## 2. Provision the Two Developer Tenants

### 2.1 Sign up

1. Go to https://developer.microsoft.com/microsoft-365/dev-program.
2. Click **Join now**, sign in with your `outlook.com` account (this becomes your *Program identity* — it does NOT become the tenant admin).
3. Choose **Instant sandbox** (recommended — pre-provisioned with 25 sample users, SPO, Exchange, Teams).
4. Set:
   - **Admin username**: `admin` (creates `admin@stara-dev.onmicrosoft.com`)
   - **Admin password**: strong password — store in your password manager
   - **Country/region**: your location
   - **Company name**: `Stara Dev` (informational only)
5. Save the tenant domain that gets assigned (e.g., `stara-dev.onmicrosoft.com`).
6. **Repeat steps 1–5 in an InPrivate/Incognito browser window** to create the second tenant. Use a different admin account and different domain (e.g., `stara-cust1.onmicrosoft.com`).

> **Tip**: Bookmark both admin sign-in URLs:
> - `https://portal.azure.com/stara-dev.onmicrosoft.com`
> - `https://portal.azure.com/stara-cust1.onmicrosoft.com`

### 2.2 Renewal reminder

Set a calendar reminder every 60 days: "Log into both dev tenants, complete any renewal task the Developer Program dashboard prompts." Otherwise tenants expire.

---

## 3. Publisher Tenant — Users, Groups, RBAC

Do this in the **publisher tenant** (`stara-dev.onmicrosoft.com`) first.

### 3.1 Create four test users

Portal → **Microsoft Entra ID** → **Users** → **+ New user** → **Create new user**.

| User principal name | Display name | Persona |
|---|---|---|
| `ra.exec@stara-dev.onmicrosoft.com` | RA Executive Test | Regulatory Executive |
| `ra.mgr@stara-dev.onmicrosoft.com` | RA Manager Test | Regulatory Manager |
| `ra.admin@stara-dev.onmicrosoft.com` | RA Admin Test | System Administrator |
| `ra.auditor@stara-dev.onmicrosoft.com` | RA Auditor Test | Read-only Auditor |

For each user, on the **Properties** tab set **Usage location** to your country (required for licensing). On the **Licenses** tab assign the **Office 365 E5** sample license (needed for SPO/OneDrive testing).

### 3.2 Create the RBAC security groups

Portal → **Microsoft Entra ID** → **Groups** → **+ New group**.

Group type = **Security**. Membership type = **Assigned**.

| Group name | Members |
|---|---|
| `ARA-RegulatoryExecutive` | `ra.exec` |
| `ARA-RegulatoryManager` | `ra.mgr` |
| `ARA-Administrator` | `ra.admin` |
| `ARA-Auditor` | `ra.auditor` |

After creation, open each group → **Overview** → copy the **Object ID** (GUID). You will paste these into `appsettings.Development.json` later.

> ✅ These group Object IDs are what the API compares against `groups` claims to enforce RBAC (SDD §4.1, §11.3).

### 3.3 Turn on group claims

App registrations must ask for `groups` claim — but if a user is in > 200 groups (unlikely in a dev tenant) Entra returns a link instead of the array. Since our dev users belong to 1 group each, plain `groups` claim is fine. See §4.4.

---

## 4. Publisher Tenant — App Registrations

We register **two** apps: the **API** (server tier) and the **WPF client**. This mirrors the SDD's `ARA.Client.Wpf` ↔ `ARA.Api` split (SDD §4.2, ADR-002).

### 4.1 Register the API — `ARA-API`

Portal → **Microsoft Entra ID** → **App registrations** → **+ New registration**.

| Field | Value |
|---|---|
| Name | `ARA-API` |
| Supported account types | **Accounts in any organizational directory (Any Microsoft Entra ID tenant - Multitenant)** |
| Redirect URI | *(leave blank)* |

Click **Register**. Record the **Application (client) ID** — this is `<API_CLIENT_ID>`.

#### 4.1.1 Expose an API

Left nav → **Expose an API** → **Add** the Application ID URI. Accept the default (`api://<API_CLIENT_ID>`).

Then **+ Add a scope**:

| Field | Value |
|---|---|
| Scope name | `access_as_user` |
| Who can consent | **Admins and users** |
| Admin consent display name | `Access ARA on behalf of the signed-in user` |
| Admin consent description | `Allows the ARA client to call the ARA API on behalf of the signed-in user.` |
| State | **Enabled** |

Click **Add scope**. Full scope value = `api://<API_CLIENT_ID>/access_as_user`.

#### 4.1.2 Define App Roles (optional but recommended)

Left nav → **App roles** → **+ Create app role** for each RBAC role. This gives you a second enforcement lever (roles claim) in addition to groups.

| Display name | Value | Allowed member types |
|---|---|---|
| Regulatory Executive | `RegulatoryExecutive` | Users/Groups |
| Regulatory Manager | `RegulatoryManager` | Users/Groups |
| Administrator | `Administrator` | Users/Groups |
| Auditor | `Auditor` | Users/Groups |

Then Portal → **Enterprise applications** → `ARA-API` → **Users and groups** → **+ Add user/group**. Assign each `ARA-*` security group to its matching app role.

#### 4.1.3 API permissions (delegated, on behalf of the user)

Left nav → **API permissions** → **+ Add a permission** → **Microsoft Graph** → **Delegated permissions**. Add:

- `User.Read` (default, already there)
- `Sites.Read.All` — read SPO sites
- `Files.Read.All` — read documents on OneDrive/SPO
- `GroupMember.Read.All` — read group membership for the group claim

Click **Grant admin consent for Stara Dev**. All rows should show green ticks.

> **Note**: `Sites.Read.All` requires admin consent — you're the admin, so it's fine. For the customer tenant (§6) the admin consent URL will be triggered on first sign-in.

#### 4.1.4 Token configuration — emit groups claim

Left nav → **Token configuration** → **+ Add groups claim** → select **Security groups** → for each of ID / Access / SAML pick **Group ID**. Save.

### 4.2 Register the WPF client — `ARA-Client`

Portal → **App registrations** → **+ New registration**.

| Field | Value |
|---|---|
| Name | `ARA-Client` |
| Supported account types | **Accounts in any organizational directory (Any Microsoft Entra ID tenant - Multitenant)** |
| Redirect URI | Platform = **Public client/native (mobile & desktop)**, URI = `ms-appx-web://microsoft.aad.brokerplugin/<will fill after WAM>` |

Actually simpler: register first with **no** redirect URI, then:

1. Left nav → **Authentication** → **+ Add a platform** → **Mobile and desktop applications**.
2. Tick the built-in URIs:
   - `https://login.microsoftonline.com/common/oauth2/nativeclient`
   - `ms-appx-web://microsoft.aad.brokerplugin/<APP_CLIENT_ID>` (WAM broker on Win10/11 — ADR-011)
3. Under **Advanced settings** set **Allow public client flows** → **Yes** (needed for WAM/device code fallback).
4. Save.

Record the **Application (client) ID** — this is `<CLIENT_APP_ID>`.

#### 4.2.1 Grant the WPF client permission to call the API

Left nav → **API permissions** → **+ Add a permission** → **My APIs** → pick `ARA-API` → **Delegated permissions** → tick `access_as_user` → **Add permissions** → **Grant admin consent**.

---

## 5. Publisher Tenant — Seed SPO Test Content

Because much of ARA's ingestion is from SharePoint / OneDrive, seed some documents so retrieval and CTD compilation have real material.

1. Sign into https://stara-dev-admin.sharepoint.com as `admin@stara-dev.onmicrosoft.com`.
2. Create a Team site: **`Regulatory-Test`**.
3. Under **Documents**, create the folder tree the ARA discovery layer expects (SDD §4.5.1):

   ```
   /Regulatory-Test/Documents/
     Product-Alpha/
       M1-Administrative/
         cover-letter.docx
         application-form.pdf
       M2-Summaries/
         quality-overall-summary.docx
         nonclinical-overview.pdf
       M3-Quality/
         3.2.S-drug-substance.docx
         3.2.P-drug-product.docx
       M4-Nonclinical/
         pharmacology-summary.pdf
       M5-Clinical/
         clinical-overview.docx
         csr-study-001.pdf
   ```

4. Upload a few small dummy files (any content — a Lorem ipsum Word doc will do; ARA will still classify them). This is enough to smoke-test discovery, arrangement, and gap analysis end-to-end.
5. Give the four test users **Read** access on `Regulatory-Test`.

---

## 6. Customer Tenant — Prep for Multi-Tenant Sign-In

Do the following in the **customer tenant** (`stara-cust1.onmicrosoft.com`). **No app registration is created here** — the whole point of multi-tenant is that the publisher's registration is provisioned on demand.

### 6.1 Create test users and RBAC groups

Repeat §3.1 and §3.2 but with only 2 users:

| UPN | Group |
|---|---|
| `ra.exec@stara-cust1.onmicrosoft.com` | `ARA-RegulatoryExecutive` |
| `ra.mgr@stara-cust1.onmicrosoft.com` | `ARA-RegulatoryManager` |

Group **Object IDs will be different from the publisher tenant**. This is the critical multi-tenant test: your API must not hard-code the publisher tenant's group IDs — it must resolve them per-tenant.

### 6.2 Recommended approach for group→role mapping across tenants

Two options; pick one:

- **Option A — App Roles (simpler, recommended)**. Because you defined App Roles in §4.1.2, the customer-tenant admin assigns their own groups/users to the app role in **Enterprise applications** → `ARA-API` after admin consent. Your API code then only checks the `roles` claim, which is tenant-agnostic.
- **Option B — Per-tenant group mapping**. Store a `TenantId → { GroupObjectId → Role }` map in your DB (`TenantConfig` table). Admin adds their tenant's group IDs post-onboarding.

> ✅ For dev testing choose **Option A** — you already set it up in §4.1.2. Then §7 becomes: admin of the customer tenant just assigns their `ARA-RegulatoryExecutive` group to the `RegulatoryExecutive` app role.

### 6.3 Seed a tiny SPO folder in the customer tenant too

Same as §5 but with a single product (`Product-Beta`, one or two files). This proves data isolation — the publisher-tenant user must NOT see Product-Beta and vice versa.

---

## 7. First Multi-Tenant Sign-In (Admin Consent Flow)

This is the moment of truth for multi-tenancy.

1. As the **customer tenant admin** (`admin@stara-cust1.onmicrosoft.com`), navigate to the admin-consent URL:

   ```
   https://login.microsoftonline.com/stara-cust1.onmicrosoft.com/adminconsent
     ?client_id=<API_CLIENT_ID>
     &redirect_uri=https://localhost
     &state=1234
   ```

2. Sign in → review the requested permissions (User.Read, Sites.Read.All, Files.Read.All, GroupMember.Read.All) → **Accept**.
3. Repeat with `client_id=<CLIENT_APP_ID>` so the WPF client is also provisioned in the customer tenant.
4. In the customer tenant, portal → **Enterprise applications** → `ARA-API` → **Users and groups** → **+ Add user/group** → assign `ARA-RegulatoryExecutive` to the `RegulatoryExecutive` app role (per §6.2 Option A).

Now `ra.exec@stara-cust1.onmicrosoft.com` can sign into the WPF client just like a publisher-tenant user — with the API receiving tokens issued by the customer tenant.

---

## 8. Local App Configuration

Add `appsettings.Development.json` in both `ARA.Api` and `ARA.Client.Wpf` projects (values below are placeholders — fill from §4).

### 8.1 `ARA.Api/appsettings.Development.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "<API_CLIENT_ID>",
    "Audience": "api://<API_CLIENT_ID>",
    "Scopes": "access_as_user"
  },
  "MultiTenant": {
    "Enabled": true,
    "AllowedTenantIds": [
      "<PUBLISHER_TENANT_ID>",
      "<CUSTOMER_TENANT_ID>"
    ]
  },
  "GraphApi": {
    "Scopes": [
      "https://graph.microsoft.com/User.Read",
      "https://graph.microsoft.com/Sites.Read.All",
      "https://graph.microsoft.com/Files.Read.All",
      "https://graph.microsoft.com/GroupMember.Read.All"
    ]
  },
  "Rbac": {
    "Mode": "AppRoles",
    "AppRoles": {
      "RegulatoryExecutive": "RegulatoryExecutive",
      "RegulatoryManager":   "RegulatoryManager",
      "Administrator":       "Administrator",
      "Auditor":             "Auditor"
    }
  }
}
```

- Use `"TenantId": "common"` so tokens from **any** tenant are accepted; then use `MultiTenant.AllowedTenantIds` (checked in a custom `TokenValidatedEvent` handler) as an allow-list.
- **Never** ship `"common"` without an allow-list — that would let *any* Entra tenant call your API.

### 8.2 `ARA.Client.Wpf/appsettings.Development.json`

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations",
    "ClientId": "<CLIENT_APP_ID>",
    "RedirectUri": "ms-appx-web://microsoft.aad.brokerplugin/<CLIENT_APP_ID>",
    "ApiScopes": [ "api://<API_CLIENT_ID>/access_as_user" ]
  },
  "Api": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

- `"TenantId": "organizations"` in the client prompts for any work/school account (blocks personal MSA) — matches BRD "corporate accounts only".

### 8.3 API startup — snippet for the multi-tenant guard

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        Configuration.Bind("AzureAd", options);
        options.TokenValidationParameters.ValidateIssuer = false; // we validate manually below
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var tid = ctx.Principal?.FindFirst("tid")?.Value;
                var allowed = Configuration.GetSection("MultiTenant:AllowedTenantIds").Get<string[]>() ?? Array.Empty<string>();
                if (string.IsNullOrEmpty(tid) || !allowed.Contains(tid, StringComparer.OrdinalIgnoreCase))
                    ctx.Fail($"Tenant '{tid}' is not allowed.");
                return Task.CompletedTask;
            }
        };
    }, options => Configuration.Bind("AzureAd", options))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(Configuration.GetSection("GraphApi"))
    .AddInMemoryTokenCaches();
```

---

## 9. Test Matrix — What to Verify

Run each scenario at least once. Mark ✅ / ❌ in the *Actual* column.

| # | Scenario | Expected | Actual |
|---|---|---|---|
| 1 | `ra.exec@stara-dev` signs in via WAM | Sign-in success, sees Executive dashboard | |
| 2 | `ra.auditor@stara-dev` tries to trigger a dossier compilation | 403 Forbidden (missing role) | |
| 3 | `ra.exec@stara-cust1` signs in for first time | Admin-consent prompt if not already granted, then success | |
| 4 | `ra.exec@stara-cust1` browses documents | Sees only Product-Beta, NOT Product-Alpha (tenant isolation) | |
| 5 | User from a **third** tenant (not in allow-list) signs in | API returns 401 with "Tenant not allowed" | |
| 6 | Kill network, sign in offline | MSAL uses cached token / graceful error (per SDD §11) | |
| 7 | Change a user's group membership → sign out → sign in | New role reflected in claims within one sign-in cycle | |
| 8 | Multi-tenant SPO discovery (§4.5.1) | Documents enumerated only from the caller's tenant SPO | |
| 9 | Compile dossier as `ra.mgr@stara-cust1` | `DossierRun` row created with correct `TenantId` FK | |
| 10 | Audit log query | `AuditEvent` rows carry the correct `tid` from token | |

---

## 10. Common Pitfalls

| Symptom | Cause | Fix |
|---|---|---|
| `AADSTS50020` — user account from external identity provider not exist in tenant | Signed into wrong tenant, or admin consent not done in target tenant | Redo §7 admin-consent flow for that tenant |
| `AADSTS65001` — user or admin has not consented | Missing consent for one of the delegated Graph scopes | Grant admin consent in **Enterprise applications** in each tenant |
| WPF client returns tokens for wrong tenant | `TenantId=common` in client — cached account from another tenant selected | Use `"TenantId": "organizations"` and pass `WithAccount(...)` explicitly, or set `WithTenantId(...)` per request |
| Groups claim missing from token | User in > 200 groups OR token config not saved | Verify §4.1.4; also switch to App Roles (Option A) to sidestep this |
| SharePoint returns 403 | Delegated `Sites.Read.All` not consented in that tenant | Redo §7 for that tenant; also verify user has SPO license (§3.1) |
| `_ClientCredentialGrant` errors when API calls Graph | You're using OBO but token audience is wrong | API must acquire an OBO token from the incoming user token; scope = `https://graph.microsoft.com/.default` in prod, or the delegated scope list in dev |

---

## 11. Cleanup / Renewal

- **90-day expiry**: Log into each dev tenant admin center at least every 60 days. The Developer Program dashboard shows a countdown and a "renew" button — one click extends it 90 days.
- **When retiring**: Delete app registrations from the publisher tenant. In each customer tenant, delete the Enterprise application entries.

---

## 12. Where This Fits in the SDD

- Realises SDD **§11 Security** (Entra ID, RBAC via groups/app roles, tenant isolation).
- Provides the concrete test bed for SDD **§13.6 Testing Strategy** — end-to-end sign-in, RBAC, multi-tenant, and SPO connector tests.
- Aligns with **ADR-002** (thin server tier) and **ADR-011** (MSAL + WAM + OBO).
- Multi-tenant guard (§8.3) implements the allow-list required by NFR-016/017.

> Next design update (optional): add §11.5 "Multi-Tenant Onboarding" to SDD summarising the admin-consent flow above and the App-Roles-vs-group-map choice, and add a `Tenant` table (`TenantId PK, DisplayName, OnboardedAt, IsActive, TokenIssuer`) to §6.2 so tenants become first-class rows the API can allow-list dynamically instead of via config.
