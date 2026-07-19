# ARA Update Guide — Customer

**Audience**: Customer's DevOps engineer.

**Frequency**: Per publisher release (typically monthly minor, quarterly major).

**Duration**: ~15 minutes for a routine (no-schema-change) release; longer for major upgrades with data backfills.

---

## Release cadence

- Publisher publishes on tags `vX.Y.Z` following semver.
  - `X` (major) — may include breaking API/DB changes; requires publisher advisory
  - `Y` (minor) — new features, backward compatible
  - `Z` (patch) — bug fixes, backward compatible
- Publisher notifies via support portal RSS + Teams/email.
- **Support policy**: only the latest two minor versions receive fixes. Customers more than one minor behind must upgrade before requesting support.

## The upgrade flow

```
1. Read release notes             → https://support.stara.example/releases/<version>
2. Review manifest breaking flags → what-if in dev first
3. Bump releaseVersion in your repo → PR review
4. Pipeline runs in UAT           → business validates (24-72h)
5. Approve prod stage             → pipeline deploys prod (slot-swap)
6. Post-deploy verification       → close change record
```

## Step-by-step

### 1. Read release notes and manifest flags

Notes URL is in the release announcement. Look for:

- **BREAKING CHANGES** section — action required
- **Schema changes** — check `manifest.migrations.breaking` and `manifest.migrations.backfillJobsRequired`
- **Client compatibility** — `manifest.minCompatibleClient` (do end users need MSIX update first?)

If `manifest.migrations.irreversible == true` or `manifest.migrations.breaking == true`, engage change management and schedule a maintenance window.

### 2. Test in UAT first

```powershell
# In your deploy repo:
git checkout -b upgrade/1.4.1
# Edit pipeline default:
#   ADO:  update the parameter default in azure-pipelines-deploy.yml
#   GHA:  no code change needed — pass releaseVersion at run time
git commit -am "Upgrade ARA to 1.4.1"
git push -u origin upgrade/1.4.1
# Open PR; wait for pipeline to run what-if in UAT
```

Review the `what-if` output posted to the PR. Look for:
- Unexpected resource deletions
- Sizing changes (SKU/capacity)
- Configuration key changes

Approve → pipeline deploys UAT → smoke tests pass → business validates.

### 3. Promote to prod

Merge the PR to `main`. On merge, or on manual trigger with `targetEnv=prod`, the pipeline:

1. Downloads verified artifacts
2. Runs `what-if` against prod → post output to PR / issue
3. Awaits approval (2 reviewers by policy)
4. Deploys Bicep (idempotent — usually no changes to infra)
5. Runs EF migrations (bundle is idempotent; skips already-applied migrations)
6. Deploys container to **staging slot**
7. Runs smoke tests on staging slot
8. Slot-swaps staging → production (transparent — no downtime)

Total change window from approval to prod live: **~5–8 minutes** for API-only releases; longer if migrations run large tables.

### 4. Post-deploy verification

```powershell
# Confirm live version
curl https://app-ara-prod-<customerCode>.azurewebsites.net/api/version
# Should return { "version": "1.4.1", "commit": "...", "deployedAt": "..." }

# Watch App Insights for anomalies (first 30 min)
# – Availability, exception rate, dependency failure rate
```

Notify RA users of new features (release notes summary works well as a Teams post).

## Rollback

If smoke tests fail or you detect issues post-swap:

```powershell
# 1. Slot-swap back (instant, < 60s)
az webapp deployment slot swap \
  --resource-group rg-ara-prod-<customerCode> \
  --name app-ara-prod-<customerCode> \
  --slot production --target-slot staging

# 2. If schema migrations already applied and are the cause:
#    Rerun pipeline with the PREVIOUS releaseVersion (e.g. 1.4.0)
#    - if migration is N-1 compatible (default), previous API works against new schema
#    - if migration is IRREVERSIBLE, rollback requires DB restore — see docs/dr.md

# 3. Open a support ticket including diagnostics bundle from
#    deploy/scripts/diagnostics-collect.ps1
```

## Client MSIX upgrade

For each release, the publisher publishes a new signed MSIX. Steps:

1. Download MSIX + `.appinstaller` from release feed (verified via manifest sha256).
2. Upload to **customer-hosted** update feed (internal Blob or SharePoint).
3. Increment `AppInstallerUri`'s Version attribute.
4. Client auto-detects new version on next launch → downloads → installs.
5. Enforce a **minimum client version** via `minCompatibleClient` in the API — the API rejects sign-ins from clients older than the manifest's floor with a friendly upgrade message.

## Skipping versions

If you're behind by multiple releases:
- **Patch-only jumps** (e.g. 1.4.0 → 1.4.3): safe, deploy the latest directly.
- **Minor jumps** (e.g. 1.4.x → 1.6.x): **do NOT skip minors**. Deploy each minor version in order in UAT to catch stacked migrations correctly. Prod can deploy the latest directly once UAT has validated the chain.
- **Major jumps**: engage publisher for a migration plan; may need dedicated professional-services engagement.

## Support boundaries

- **Emergency hotfixes**: publisher may issue an out-of-band `X.Y.Z+hotfix.N` build. Deploy through the same pipeline; skip UAT only with explicit publisher sign-off.
- **Version freezes**: freeze customer-side by pinning `releaseVersion` in `parameters.<env>.json` and disabling auto-triggered pipeline runs.
- **End of support**: publisher will announce EOS ≥ 6 months before dropping support for a minor version. Customers must upgrade before EOS.
