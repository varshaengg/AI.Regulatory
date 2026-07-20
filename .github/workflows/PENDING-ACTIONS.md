# GitHub Actions — pending setup actions

**Status:** The GitHub Actions workflows are committed and functionally equivalent
to the ADO pipelines, but they have not been used to deploy dev yet. All CI
today runs from Azure DevOps. This document lists everything that needs to be
done, in order, when you're ready to switch (or add) GH Actions as a
deployment path.

Complete the sections in order — each depends on the ones above.

---

## 1. Federated credentials (OIDC login)

The workflows use `azure/login@v2` with OIDC — no client secret required for
`az` CLI calls. Federation must be set up for **each** ref you deploy from.

App registration used for CI (dev sub): `sp-ara-github-cd`
Object id: `65139a46-578f-41d5-b651-7f3f2b5d2f23`
Tenant: `varshasslive.onmicrosoft.com` (`15a19fa8-a0c4-4790-93b5-bb20d970cfdf`)
Subscription (dev): `96bea808-f9d4-428a-97d1-3ed1dcf12f63`

On the app registration → **Certificates & secrets → Federated credentials → Add**:

| Field     | Value                                                    |
|-----------|----------------------------------------------------------|
| Scenario  | GitHub Actions deploying Azure resources                 |
| Org       | `varshaengg`                                             |
| Repo      | `AI.Regulatory`                                          |
| Entity    | Environment                                              |
| Env name  | `ara-dev` (repeat for `ara-test`, `ara-uat`, `ara-prod`) |

If you also want manual `workflow_dispatch` from any branch:

| Entity    | Branch  |
|-----------|---------|
| Branch    | `main`  |

The `azure/login@v2` step will fail with `AADSTS70021` if federation is missing.

---

## 2. Repository secrets

`Settings → Secrets and variables → Actions → New repository secret`:

| Secret                     | Value                                                        | Used by |
|----------------------------|--------------------------------------------------------------|---------|
| `AZURE_CLIENT_ID`          | `<sp-ara-github-cd application (client) id>`                 | infra, code, sql |
| `AZURE_TENANT_ID`          | `15a19fa8-a0c4-4790-93b5-bb20d970cfdf`                       | infra, code, sql |
| `AZURE_SUBSCRIPTION_ID`    | `96bea808-f9d4-428a-97d1-3ed1dcf12f63`                       | infra, code, sql |
| `DEPLOYER_OBJECT_ID`       | `65139a46-578f-41d5-b651-7f3f2b5d2f23`                       | infra (KV admin grant) |
| `PUBLISHER_ACR_LOGIN`      | empty for full-stack builds                                  | infra |
| `AZURE_CLIENT_SECRET`      | client secret from app reg (**required for SQL pipeline**)  | sql |

> **Why `AZURE_CLIENT_SECRET` for SQL?** `sqlpackage` cannot consume the
> OIDC-issued JWT that `azure/login` gives us — it needs a raw
> `TargetUser`/`TargetPassword` pair for `AzureAuthType=ClientSecret`. The
> other two workflows (infra, code) run entirely through `az` CLI, which is
> OIDC-happy, so they don't need the client secret.

Steps to create the client secret if you decide to enable SQL from GH:

1. App reg → **Certificates & secrets → Client secrets → New client secret**
2. Description: `github-actions-sqlpackage`, expiry: 12 months (rotate)
3. Copy the **Value** immediately (only shown once)
4. Save as repo secret `AZURE_CLIENT_SECRET`
5. Set a calendar reminder to rotate before expiry

---

## 3. Repository variables

`Settings → Secrets and variables → Actions → Variables tab`:

| Variable          | Value             |
|-------------------|-------------------|
| `PROJECT`         | `ra`              |
| `AZURE_REGION`    | `southindia`      |
| `IMAGE_TAG`       | *(leave empty)*   |

Variables (not secrets) show up in job logs — safe for these.

---

## 4. Environments

`Settings → Environments → New environment`. Create one per target env:

- `ara-dev`
- `ara-test` (later)
- `ara-uat`  (later)
- `ara-prod` (later)

Optional per environment:
- **Required reviewers** — add yourself to gate prod deployments.
- **Deployment branch policy** — restrict to `main`.

The workflows reference `environment: ara-${{ inputs.targetEnv || 'dev' }}` so
this must exist before the deploy job can start.

---

## 5. First-run order (once above is done)

1. **Infra** — `Actions → ARA - Infra → Run workflow → dev`
   Wait for it to complete (~5-8 min). It provisions RG + all resources.

2. **SQL bootstrap (one-time, manual)** — before the SQL workflow can run,
   the CI SP must be a contained SQL user. Because the SQL server is
   AAD-only and the admin is a user account (`varshass@live.com`), this
   grant cannot be self-serviced by CI:

   Connect via SSMS/Azure Data Studio (Entra MFA) to
   `sql-ra-dev-cus.database.windows.net`, database `ara`, and run:
   ```sql
   CREATE USER [sp-ara-github-cd] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_owner ADD MEMBER [sp-ara-github-cd];
   ```

3. **SQL** — `Actions → ARA - SQL → Run workflow → dev`
   Publishes the DACPAC and seeds reference data.

4. **Code** — `Actions → ARA - Code → Run workflow → dev`
   Builds SPA + API, deploys zips.

---

## 6. Known gaps between ADO and GH workflows

| Item | ADO | GH | Note |
|---|---|---|---|
| Infra deploy approval gate | Environment approval | Environment required reviewers | Configure per env, section 4 |
| Concurrency guard | Pipeline default (one at a time) | `concurrency:` block in YAML | Already set |
| Slot swap on code deploy | Yes | Yes | Both check for `staging` slot; skip if absent |
| Live-value preservation for app-settings | pwsh via `AzureCLI@2` | pwsh via `azure/login` + pwsh step | Uses same script |
| SQL pipeline auth | Uses `addSpnToEnvironment=true` (SP secret auto-injected) | Needs explicit `AZURE_CLIENT_SECRET` | Section 2 |

---

## 7. When you're ready to switch off ADO

If you eventually want GH Actions to be the sole CI:

1. Verify all three GH pipelines have run successfully for dev at least once.
2. In ADO, disable the three pipelines (Pipelines → ⋮ → Settings → Disable).
   Don't delete — keeps history + easy rollback path.
3. Remove the ADO service connection `sc-ara-dev` (Project settings → Service
   connections) if you'll never use ADO again.
4. Rotate the client secret from step 2 above (it's only needed if SQL pipeline
   ever runs from GH; if you keep SQL on ADO, delete the GH `AZURE_CLIENT_SECRET`).
