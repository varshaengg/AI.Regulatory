# App-settings config files

Every App Service deployed by the Bicep template can have up to two config files
in this folder. **The filename determines which app the settings land on**.

## File name pattern

```
appsettings.<feature>.<scope>.<env>.json
```

| Segment  | Meaning                                                                                                | Example                                        |
|----------|--------------------------------------------------------------------------------------------------------|------------------------------------------------|
| feature  | Matches `feature` in `deploy/bicep/parameters.<env>.json` -> resources[]. App name is `app-<namePrefix>-<feature>`. | `api`, `web`, `sharedapi`, `dossierapi`...     |
| scope    | Who owns the values (see below).                                                                        | `infra` or `code`                              |
| env      | Deployment environment.                                                                                | `dev`, `test`, `uat`, `prod`                   |

## Scope: `infra` vs `code`

| Scope   | Who applies it          | Typical settings                                                             | Change flow                                                       |
|---------|-------------------------|------------------------------------------------------------------------------|-------------------------------------------------------------------|
| `infra` | Infra pipeline via Bicep `extraAppSettingsMap` | Cors, EntraId, KeyVault URI, ASPNETCORE_ENVIRONMENT, WebApp.PublicUrl, other platform/security settings | Edit file -> run **infra** pipeline. Reset on every infra run.   |
| `code`  | Code pipeline via `apply-appsettings.ps1` (upsert) | Feature flags, `Data__IsMocked`, behaviour toggles                          | Edit file -> run **code** pipeline. Preserved across infra runs. |

**Preservation:** the infra pipeline's `build-extra-app-settings.ps1` reads live
values of code-owned keys and passes them into `extraAppSettingsMap` too, so
Bicep's collection-replace never wipes them. Cold start uses the seed values
from the code file.

## Adding a new App Service (e.g. `sharedapi`)

1. Add to `deploy/bicep/parameters.<env>.json`:
   ```json
   { "kind": "appService", "feature": "sharedapi" }
   ```
2. Create the config file(s) as needed:
   ```
   appsettings.sharedapi.infra.dev.json
   appsettings.sharedapi.code.dev.json
   ```
3. Run infra pipeline (creates the app + applies infra settings).
4. Run code pipeline (applies code settings).

No pipeline or Bicep-module changes required.

## Current files

| File                                    | Feature | Scope | Owned settings                                            |
|-----------------------------------------|---------|-------|-----------------------------------------------------------|
| `appsettings.api.infra.dev.json`        | `api`   | infra | ASPNETCORE_ENVIRONMENT, Cors, EntraId, WebApp.PublicUrl   |
| `appsettings.api.code.dev.json`         | `api`   | code  | Data__IsMocked                                            |
| `appsettings.web.infra.dev.json`        | `web`   | infra | (empty — placeholder; SPA gets settings baked at build time via `spa.dev.env`) |
| `spa.dev.env`                           | `web`   | build | VITE_* build-time envs sourced by `npm run build`         |

## Anti-patterns

- **Don't put the same key in both `.infra.` and `.code.` files** — the infra
  scope always wins (Bicep replaces the collection). Pick one owner per key.
- **Don't put the same key in a Bicep module *and* an `.infra.` file** — Azure
  rejects the deploy with a misleading *"Invalid values supplied for Azure
  Files related app settings"* error (this is Azure's cryptic message for
  duplicate names in `siteConfig.appSettings`).
