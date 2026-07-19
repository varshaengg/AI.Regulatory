# Cross-Repo Architecture — Aggregation Rules

Use these rules to author repo-level architecture docs so they can be stitched into a virtual monorepo view when multiple repos are opened in a single VS Code workspace.

## Required Metadata (frontmatter at top of repo architecture.md)

```yaml
---
repo: "<org>/<repo>"
service: "<logical service name>"
domain: "<bounded context/domain>"
owners: ["team@contoso", "@github-handle"]
environment: ["DEV", "PPE", "PROD"]
---
```

## Endpoint Catalog Schema

Each repo must maintain an "Endpoint Catalog" table near the top of `docs/architecture/architecture.md`:

- Columns: Endpoint ID | Route | Verb | Version | Domain | Dependencies | Owner
- Endpoint ID namespace: `<repoSlug>.<service>.<version>.<resource>.<verb>`

## Dependency Metadata

For each endpoint section, add a "Components & Dependencies" list that names external services (e.g., Azure AI Search index name, SQL database, Storage containers) and cross-repo calls (format: `<repo>/<service>#<endpointId>`).

## Aggregation/Stitching Rules

- Index all `architecture.md` files and extract:
  - Frontmatter (repo, service, domain, owners, environment)
  - Endpoint Catalog rows
  - Anchored endpoint sections (BEGIN/END ENDPOINT:ID) with dependencies
- Build a global graph:
  - Nodes: services and endpoints
  - Edges: dependency references and cross-repo calls
- Conflict handling:
  - Duplicate Endpoint IDs across repos → flag as error and require rename
  - Divergent contracts for referenced endpoint IDs → mark as drift

## Monorepo Views (virtual)

Generate (on demand) summary views:
- Service dependency map (ASCII) for the open workspace
- Endpoint index across repos (filterable by domain/service)
- Cross-repo risk report (missing owners, missing SLOs, drift/conflicts)

## Constraints and Consistency

- Follow per-repo constraints in architecture instructions
- Keep Endpoint IDs stable; avoid breaking anchors or permalinks
- Prefer explicit versioning (v1/v2) for contracts

## Critical Analysis Gate (must)

Before generating or stitching cross-repo views, write a Proposal (scope, repos affected, risks) and run the Critical Analyzer. Incorporate prioritized action items and include a short "Critical Analysis Summary" plus deltas in the output/PR.

## Review Checklist

- [ ] Repo frontmatter present and valid
- [ ] Endpoint Catalog complete and kept in sync
- [ ] Dependencies and cross-repo references use the required formats
- [ ] No duplicate Endpoint IDs across repos (as far as known)
- [ ] Contracts versioned and anchors intact
- [ ] Critical analysis performed and findings incorporated
