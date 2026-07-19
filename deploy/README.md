# ARA Deployment (`/deploy/`)

This folder is the **customer-facing deployment package** for the AI Regulatory Assistant. It follows the **"Publisher builds, Customer deploys"** BYOC model described in SDD §11 and ADR-018.

## Layout

```
deploy/
├── bicep/                          Infrastructure-as-Code (Azure)
│   ├── main.bicep                  Subscription-scope entry
│   ├── resources.bicep             Resource-group-scope orchestration
│   ├── modules/                    Per-resource modules
│   │   ├── monitoring.bicep
│   │   ├── keyvault.bicep
│   │   ├── storage.bicep
│   │   ├── sql.bicep
│   │   ├── search.bicep
│   │   ├── openai.bicep
│   │   └── appservice.bicep
│   └── parameters.example.json     Copy → parameters.<env>.json
│
├── pipelines/                      Deployment pipelines
│   ├── ci.yml                      PUBLISHER — CI on every commit
│   ├── release.yml                 PUBLISHER — releases on tag v*
│   ├── deploy.ado.yml.template     CUSTOMER — Azure DevOps (tier 1)
│   └── deploy.gha.yml.template     CUSTOMER — GitHub Actions (tier 2)
│
├── scripts/                        Operator scripts (PowerShell 7+)
│   ├── bootstrap-appreg.ps1        Customer Entra admin runs once
│   ├── seed-templates.ps1          Post-deploy — load CTD catalog
│   └── diagnostics-collect.ps1     Support-bundle generator
│
└── docs/                           Runbooks
    ├── install-guide.md
    ├── update-guide.md
    └── airgap-install-guide.md
```

## Who uses which files

| Actor | Files they interact with |
|---|---|
| Publisher engineering | `bicep/**` (author), `pipelines/ci.yml`, `pipelines/release.yml` |
| Publisher release manager | `pipelines/release.yml` |
| Customer Entra admin | `scripts/bootstrap-appreg.ps1`, `docs/install-guide.md` |
| Customer DevOps engineer | `bicep/parameters.example.json`, `pipelines/deploy.ado.yml.template` OR `pipelines/deploy.gha.yml.template`, `docs/install-guide.md`, `docs/update-guide.md` |
| Customer ARA administrator | `scripts/seed-templates.ps1`, `scripts/diagnostics-collect.ps1` |
| Customer airgapped ops | `docs/airgap-install-guide.md` |

## Status

Scaffolded in SDD v1.4. Bicep modules ship as **starter templates** — parameters and structure are correct; production-hardening (private endpoints, customer-managed keys, diagnostic settings wiring) is tracked in follow-up work items.
