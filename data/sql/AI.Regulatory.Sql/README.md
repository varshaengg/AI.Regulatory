# AI.Regulatory.Sql — DACPAC schema project

SDK-style SQL project (`Microsoft.Build.Sql`) that builds cross-platform on Linux CI runners.

## What lives here

| Item | Purpose |
|---|---|
| `AI.Regulatory.Sql.sqlproj` | SDK-style project — builds a `.dacpac` |
| `dbo/Tables/*.sql`          | Schema — one file per table |
| `Script.PostDeployment.sql` | Idempotent seed (MERGE) + optional API-MI grant |

## Local build

```powershell
dotnet build data/sql/AI.Regulatory.Sql
# → data/sql/AI.Regulatory.Sql/bin/Debug/AI.Regulatory.Sql.dacpac
```

## Local publish (against dev)

Requires `sqlpackage` (`dotnet tool install -g Microsoft.SqlPackage`) and that your AAD identity is the SQL admin (currently: `varshass@live.com`).

```powershell
$fqdn = "sql-ra-dev-cus.database.windows.net"    # from `az deployment sub show`
$dac  = "data/sql/AI.Regulatory.Sql/bin/Debug/AI.Regulatory.Sql.dacpac"

sqlpackage `
  /Action:Publish `
  /SourceFile:$dac `
  /TargetServerName:$fqdn `
  /TargetDatabaseName:ara `
  /p:BlockOnPossibleDataLoss=false `
  /Variables:ApiManagedIdentityName=app-ra-dev-sin-api `
  /ua:True                     # Universal AAD auth (interactive)
```

For CI, the ADO/GH pipelines use `AAD Service Principal` auth (deployer SP is a contained SQL user, added post-first-deploy).

## Table map

| Table              | Purpose |
|---|---|
| `Persona`          | Named role (Admin, RaLead, RaAuthor, RaReviewer) |
| `Feature`          | Product surface (UserManagement, DossierManagement, ...) |
| `Permission`       | Verb (Read, Write, Review, Admin) |
| `PersonaPermission`| Default matrix: what each persona can do per feature |
| `AppUser`          | Users granted access, keyed on AAD object id |
| `UserPersona`      | Which persona(s) each user has |

Effective permissions for a user = union over their personas of `PersonaPermission`.

## Adding a new feature

1. Add a row to the `Feature` MERGE in `Script.PostDeployment.sql`.
2. Extend the persona matrix in the same file.
3. Re-run pipeline. MERGE keeps the schema idempotent — no manual SQL required.
