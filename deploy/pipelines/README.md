# Deployment pipelines

Two functionally-equivalent CD pipelines live in this repo. Both provision
infra from `deploy/bicep/main.bicep`, apply per-env app-settings from
`deploy/config/appsettings.<app>.<env>.json` via
`deploy/scripts/apply-appsettings.ps1`, deploy the API + Web zips, and smoke test.

Each CI system has **two independent pipelines** — one for infra and one for
app code — so you can iterate on the SPA/API without re-running Bicep, and
vice versa.

| Purpose | GitHub Actions | Azure DevOps |
|---|---|---|
| Infra (Bicep what-if + deploy) | `.github/workflows/deploy-infra.yml` | `deploy/pipelines/deploy-infra.ado.yml` |
| Code (build + zip deploy + smoke + swap) | `.github/workflows/deploy-code.yml` | `deploy/pipelines/deploy-code.ado.yml` |

Path filters:

- **Infra** pipelines trigger on `deploy/bicep/**` (plus their own YAML).
- **Code** pipelines trigger on `api/**`, `src/**`, `deploy/config/**`,
  `deploy/scripts/**`, `package*.json` (plus their own YAML).

Cold start order: **run infra first**, then code. The code pipeline resolves
resource names by reading the outputs of the most recent
`ara-<env>-*` subscription-scope deployment; it fails fast with a clear
message if none exists yet.

Sub-scope Bicep deployments are always **incremental** — resources removed
from `main.bicep` are not automatically deleted. Prune manually with
`az resource delete` when needed.

Pick whichever CI system your org is standardised on — you can enable either
or both. They read the same Bicep, config, and scripts.

---

## GitHub Actions

**Setup (one-time)**

1. In Entra ID (tenant `varshasslive.onmicrosoft.com` for the current dev sub),
   create an App Registration used only for CI (e.g. `sp-ara-github-cd`).
2. Add a **Federated credential**:
   - Issuer: `https://token.actions.githubusercontent.com`
   - Subject: `repo:<owner>/<repo>:ref:refs/heads/main`
     (add one per branch/environment you deploy from)
   - Audience: `api://AzureADTokenExchange`
3. Grant the app **Contributor** on the target subscription.
4. In GitHub → repo → **Settings → Secrets and variables → Actions**:
   - Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
     `DEPLOYER_OBJECT_ID` (object id of the CI SP).
   - Variables: `PROJECT` (e.g. `ra`), `AZURE_REGION` (e.g. `southindia`),
     `PUBLISHER_ACR_LOGIN` (empty for stack builds), `IMAGE_TAG` (same).

**Trigger:** push to `main` or run manually via *Actions → Deploy → Run workflow*.

---

## Azure DevOps

**Setup (one-time)**

1. In ADO → Project settings → **Service connections** → *New*:
   - **GitHub** connection so the pipeline can read this repo.
   - **Azure Resource Manager** connection (Workload Identity Federation
     recommended) pointing at the target subscription; name it
     `sc-ara-<env>` (e.g. `sc-ara-dev`). This connection is what
     `azureSubscription:` variables in the YAML reference.
2. In ADO → Pipelines → **New pipeline** → GitHub → pick this repo → choose
   *Existing YAML file* → `deploy/pipelines/deploy.ado.yml`.
3. Add a **Variable Group** (or pipeline variables) supplying the vars the
   YAML expects: `azureSubscription`, `project`, `azureRegion`,
   `publisherAcrLoginServer`, `imageTag`, `deployerObjectId`.

**Trigger:** push to `main` (auto-detected by ADO) or run manually from
Pipelines UI with `targetEnv` parameter.

---

## Shared assets

| Path | Purpose |
|---|---|
| `deploy/bicep/main.bicep` (+ `resources.bicep` + `modules/`) | Infra as code |
| `deploy/bicep/parameters.<env>.json` | Per-env Bicep parameter values |
| `deploy/config/appsettings.api.<env>.json` | API app-settings applied by both pipelines |
| `deploy/config/appsettings.web.<env>.json` | Optional Web app-settings (auto-skipped if absent) |
| `deploy/scripts/apply-appsettings.ps1` | Reusable script called by both pipelines |
