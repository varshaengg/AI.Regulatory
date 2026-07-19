# ARA Install Guide — Customer

**Audience**: Customer's DevOps engineer + Entra ID admin. First-time installation into a customer-owned Azure subscription.

**Duration**: ~2 hours end-to-end (mostly waiting for Bicep to complete).

**Prerequisites**:
- An Azure subscription with **Contributor** rights for the deployer.
- **Global Administrator** or equivalent in the customer's Entra tenant (needed once for `bootstrap-appreg.ps1` and admin consent).
- A quota-approved region (Sweden Central recommended for EU customers).
- Azure DevOps organisation *or* GitHub organisation with Actions enabled.
- PowerShell 7+ locally and Azure CLI 2.60+.

---

## Step 1 — Publisher provisions credentials

Publisher (Stara) issues to you:

- **ACR pull token** (`PUBLISHER_ACR_TOKEN_USER` + `PUBLISHER_ACR_TOKEN`) — rotated yearly
- **Release feed SAS URL** (`PUBLISHER_FEED_SAS`) — rotated quarterly
- **Publisher signing public key** (PEM file `publisher-public.pem`) — pinned in your deploy repo
- **Starter repo link** — clone URL of `stara/ara-customer-starter`

## Step 2 — Fork the starter repo

```powershell
git clone https://github.com/stara/ara-customer-starter.git ara-deploy
cd ara-deploy
# Then push to your own git host
git remote set-url origin https://<your-git-host>/<your-org>/ara-deploy.git
git push -u origin main
```

The starter contains:
- `bicep/` — copy of ARA's Bicep templates (§11.2)
- `parameters.example.json` — template for your env-specific parameter files
- `pipelines/deploy.ado.yml.template` and `.../deploy.gha.yml.template` — pick one
- `scripts/bootstrap-appreg.ps1`
- `scripts/seed-templates.ps1`

## Step 3 — Create the deploy service principal

You need a service principal (SP) your pipeline uses to authenticate to Azure. **Prefer OIDC federated credential (no secrets).**

```powershell
$sp = az ad sp create-for-rbac --name sp-ara-deploy --skip-assignment | ConvertFrom-Json

# Give it Contributor + User Access Admin on the target subscription
az role assignment create --assignee $sp.appId --role Contributor          --scope /subscriptions/<sub-id>
az role assignment create --assignee $sp.appId --role "User Access Administrator" --scope /subscriptions/<sub-id>

# Add federated credential (adjust for your ADO org or GitHub repo)
# Azure DevOps:
az ad app federated-credential create --id $sp.appId --parameters '{
  "name":"ado-federated",
  "issuer":"https://vstoken.dev.azure.com/<ADO-ORG-ID>",
  "subject":"sc://<ADO-ORG>/<PROJECT>/sc-ara-prod",
  "audiences":["api://AzureADTokenExchange"]
}'
```

Note the SP's `appId` — you'll paste it into the pipeline as `AZURE_CLIENT_ID`.

## Step 4 — Create RBAC security groups in Entra

Portal → Microsoft Entra ID → Groups → **+ New group** (Security). Create:

| Group name | Purpose |
|---|---|
| `ARA-RegulatoryExecutive` | Regulatory execs — read + limited write |
| `ARA-RegulatoryManager`   | Regulatory managers — full write |
| `ARA-Administrator`       | ARA admins — templates, RBAC, config |
| `ARA-Auditor`             | Read-only auditors |

Copy each group's **Object ID** — you'll paste into `parameters.<env>.json` and use later for App Role assignment.

Also create `SQL-ARA-Admins` group (put your DBAs in it) — this becomes the SQL server's AAD admin.

## Step 5 — Run `bootstrap-appreg.ps1`

Run **once** in the customer Entra tenant, as Global Admin:

```powershell
cd deploy/scripts
Install-Module Microsoft.Graph -Scope CurrentUser        # if not present
./bootstrap-appreg.ps1 -TenantId <your-tenant-id> -DisplayNamePrefix 'ARA-Prod'
```

Output file `appreg-output.json` contains:
- `apiClientId`   → paste into `parameters.<env>.json` (used by App Service)
- `clientClientId` → embedded into MSIX first-run wizard config
- `apiScope`      → `api://<apiClientId>/access_as_user`

Then in the portal:
1. **Enterprise applications → ARA-Prod-API → Permissions → Grant admin consent** (for your tenant).
2. **Enterprise applications → ARA-Prod-API → Users and groups → +Add**: assign each `ARA-*` group to the matching App Role (`RegulatoryExecutive`, etc.).

## Step 6 — Fill parameter files

Copy the example and edit per environment:

```powershell
cd deploy/bicep
Copy-Item parameters.example.json parameters.prod.json
# edit — populate customerCode, region, group object IDs, api client id, etc.
```

Commit the parameter files into your repo. Do NOT commit tokens or SAS URLs — those live in pipeline secrets.

## Step 7 — Configure the pipeline

### If Azure DevOps

1. Create Environments named `ara-dev`, `ara-uat`, `ara-prod`. Add required reviewers on `ara-prod` (min 2).
2. Create Variable Groups:
   - `ara-customer-config`: `customerCode`, `azureRegion`
   - `ara-publisher-credentials`: `PUBLISHER_ACR_TOKEN`, `PUBLISHER_ACR_TOKEN_USER`, `PUBLISHER_FEED_SAS`
3. Create a Service Connection per env: `sc-ara-dev`, `sc-ara-uat`, `sc-ara-prod` (OIDC federated).
4. Copy `deploy.ado.yml.template` → `azure-pipelines-deploy.yml`. Commit + push.

### If GitHub Actions

1. Create Environments (`dev`, `uat`, `prod`) — configure required reviewers.
2. Add repository/environment secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `CUSTOMER_CODE`, `AZURE_REGION`, `PUBLISHER_FEED_SAS`, `PUBLISHER_ACR_TOKEN`.
3. Copy `deploy.gha.yml.template` → `.github/workflows/deploy.yml`. Commit + push.

## Step 8 — First deployment

Trigger the workflow:
- ADO: Pipelines → ARA-Deploy → Run → pick `targetEnv=uat`, `releaseVersion=1.4.0`
- GitHub: Actions → ARA-Deploy → Run workflow

The pipeline will:
1. Register providers, check quotas
2. Download release manifest + Bicep bundle + EF migration bundle
3. Post `what-if` output — **review it**
4. On approval: run Bicep deploy (~15–25 min)
5. Run EF migrations
6. Seed CTD template catalog
7. Deploy container to staging slot
8. Smoke test → slot swap

## Step 9 — Verify

```powershell
# Health probes
$appUrl = "https://app-ara-prod-<customerCode>.azurewebsites.net"
curl "$appUrl/health"       # 200 OK
curl "$appUrl/api/version"  # returns installed release version

# Test sign-in
# Install the MSIX from publisher release feed on a test machine
# First-run wizard prompts for:
#   Tenant ID:  <your-tenant-id>
#   API URL:    $appUrl
#   Client ID:  <clientClientId from appreg-output.json>
# Sign in as a member of ARA-RegulatoryExecutive → should reach dashboard.
```

## Step 10 — Rollout

1. Distribute MSIX via Intune Company Portal or GPO.
2. Onboard users: assign them to the appropriate `ARA-*` security group.
3. Point the CTD template catalog upload UI at the seeded templates.
4. Configure SharePoint site URLs for discovery (in ARA Admin console).

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `az deployment sub what-if` shows "region not enabled for Azure OpenAI" | Region choice | Change `location` to one of eastus2, swedencentral, francecentral |
| EF migrate step fails with SQL 40515 | AAD admin group not populated | Add your deployer or a DBA into `SQL-ARA-Admins` |
| App Service 502 on first hit | Cold start, container pulling | Wait 60s and retry; verify `linuxFxVersion` = correct tag |
| Sign-in shows AADSTS65001 | Admin consent not granted | Step 5 last part — grant admin consent |

For anything else: run `deploy/scripts/diagnostics-collect.ps1` and open a support ticket.
