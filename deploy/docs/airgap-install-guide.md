# ARA Airgapped Install Guide — Customer

**Audience**: Customers whose Azure subscription cannot reach public internet (or specifically cannot reach publisher endpoints `stara.azurecr.io` and `starareleases.blob.core.windows.net`).

**Precondition**: You have an Azure subscription with private endpoints and firewall policies that forbid outbound calls to publisher-hosted artifacts.

## Concept

Publisher issues you a **release bundle** — a signed tarball containing all artifacts for one release. Your team side-loads it into your own private ACR and Storage account, and the deploy pipeline reads from those instead of publisher endpoints.

```
Publisher                        Sneakernet / secure transfer                  Customer
─────────                        ─────────────────────────                     ─────────
release.yml → bundle .tar.gz ──────────────────────────────────────────▶  side-load script
                                                                              │
                                                                              ├→ private ACR
                                                                              ├→ private Storage
                                                                              └→ deploy pipeline (airgap mode)
```

## Bundle contents

`ara-release-bundle-<version>.tar.gz` contains:

```
manifest/
  release-<version>.json          Signed manifest
  publisher-public.pem            Signing key (also verify out of band via publisher fingerprint)
images/
  ara-api-<version>.tar           OCI image tarball (docker save)
  ara-workers-<version>.tar       OCI image tarball
bicep/                            Full Bicep tree
scripts/                          bootstrap-appreg, seed-templates, diagnostics-collect
migrations/
  ara-efbundle-<version>.exe      Self-contained EF migration bundle (linux-x64)
msix/
  ARA-Client-<version>.msix       Signed client installer
sbom/
  ara-sbom-<version>.spdx.json
docs/
  release-notes-<version>.md
  install-guide.md
  update-guide.md
  airgap-install-guide.md         (this file)
```

## Sideload procedure

### One-time — set up your private mirrors

You need a private ACR and Storage account inside your subscription. These can be provisioned via a small Bicep companion or manually:

```powershell
# ACR (Premium recommended for private link support)
az acr create -n acrArtifactsAra -g rg-shared-tooling --sku Premium --admin-enabled false

# Storage for Bicep + MSIX + manifest
az storage account create -n storagearaartifacts -g rg-shared-tooling --sku Standard_ZRS
az storage container create -n releases --account-name storagearaartifacts --auth-mode login
```

Grant your deploy SP:
- `AcrPull` and `AcrPush` on the ACR
- `Storage Blob Data Contributor` on the storage account

### Per release — sideload

```powershell
# 1. Verify the bundle signature (out of band with publisher fingerprint before proceeding)
$ver = '1.4.0'
tar -xzf ara-release-bundle-$ver.tar.gz -C ./ara-bundle
cd ara-bundle

# 2. Verify manifest signature
openssl dgst -sha256 -verify manifest/publisher-public.pem `
    -signature manifest/release-$ver.json.sig `
    manifest/release-$ver.json
# Should print: Verified OK

# 3. Import container images into private ACR
docker load -i images/ara-api-$ver.tar
docker load -i images/ara-workers-$ver.tar
az acr login -n acrArtifactsAra
docker tag stara.azurecr.io/ara-api:$ver     acrArtifactsAra.azurecr.io/ara-api:$ver
docker tag stara.azurecr.io/ara-workers:$ver acrArtifactsAra.azurecr.io/ara-workers:$ver
docker push acrArtifactsAra.azurecr.io/ara-api:$ver
docker push acrArtifactsAra.azurecr.io/ara-workers:$ver

# 4. Upload manifest + Bicep bundle + EF migration bundle to private Storage
az storage blob upload-batch `
    --account-name storagearaartifacts `
    --destination "releases/$ver" `
    --source ./ara-bundle `
    --auth-mode login
```

### Point the deploy pipeline at private mirrors

In your deploy pipeline configuration:

```yaml
variables:
  # Airgap: point everything to internal mirrors
  publisherAcrLoginServer: 'acrArtifactsAra.azurecr.io'
  publisherReleaseFeedBaseUrl: 'https://storagearaartifacts.blob.core.windows.net/releases'
  airgapMode: 'true'
```

And in `parameters.<env>.json`:

```json
"publisherAcrLoginServer": { "value": "acrArtifactsAra.azurecr.io" }
```

The pipeline's `pull-artifacts` stage detects `airgapMode=true` and reads from the internal Storage account using AAD auth (no SAS needed). The image mirror step is skipped because images are already in your ACR.

### MSIX distribution

Copy `ARA-Client-<version>.msix` and `.appinstaller` into your internal software distribution (Intune Company Portal, SCCM, or internal Storage + GPO). Client updates from your feed, not publisher's.

## Verification

Same verification steps as `install-guide.md` Step 9. Additionally, verify no outbound calls to publisher endpoints:

```powershell
# Should return connection refused / policy deny
Invoke-WebRequest https://stara.azurecr.io -TimeoutSec 5
```

## Update flow (airgap)

Publisher publishes → you receive new bundle → sideload procedure again → deploy pipeline runs (unchanged flow, just against your private mirrors).

## Support

Publisher still has zero access. Support model is identical: run `diagnostics-collect.ps1`, upload the encrypted bundle to publisher's support portal via your normal secure file-transfer channel (usually the same channel used to receive release bundles).

## Trade-offs

| Concern | Airgap install |
|---|---|
| Update latency | Days to weeks (bundle transfer + sideload) vs. minutes for connected |
| Setup complexity | Higher (private ACR + Storage + PowerShell sideload) |
| Ongoing ops cost | Bundle handling per release, integrity checks |
| Security posture | Highest — no outbound calls, full artifact custody |
| Publisher help during upgrade | None (they can't reach in) — plan carefully |
