---
name: cross-repo-development
description: "Framework-agnostic cross-repo implementation and architecture aggregation workflows. Multi-step design-driven implementation across multiple repositories and architecture stitching. USE FOR: cross-repo implementation, multi-repo workspace, monorepo architecture, architecture aggregation, endpoint catalog, service dependency map, cross-repo stitching, design-to-code workflow, multi-service implementation. DO NOT USE FOR: single-repo coding standards (use language instructions), architecture design for a single service (use architecture-design skill), compliance checks (use compliance-enforcement skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Cross-Repo Development Skill

These instructions guide automated and human implementers to turn design documents into working code across multiple repositories, and to aggregate architecture metadata across repos in a workspace.

## When to Activate

- "implement across repos"
- "cross-repo changes"
- "multi-repo workspace"
- "architecture aggregation"
- "endpoint catalog"
- "service dependency map"
- "stitch architecture docs"
- Working with multiple repos in a single VS Code workspace

## When NOT to Use

- Single-repo coding standards (use language-specific instructions)
- Single-service architecture design (use `architecture-design` skill)
- Compliance reviews (use `compliance-enforcement` skill)
- Deployment execution (use `microsoft-engineering` skill)

## Part 1: Cross-Repo Implementation

See [references/cross-repo-implementation.md](references/cross-repo-implementation.md) for the full implementation workflow.

### Quick Reference

#### Discovery & Design Intake

- Inputs: `MONOREPO_ARCHITECTURE.md`, `**/docs/design/*.md`, `**/docs/architecture/*.md`
- Extract: Services/endpoints, data contracts, dependencies, observability expectations

#### Framework/Build Detection

Detect frameworks by file presence:

- .NET: `*.sln`, `*.csproj` → `dotnet` build/test
- Node.js: `package.json` → `npm|yarn|pnpm`
- Python: `pyproject.toml|requirements.txt` → `pip|uv|poetry`
- Java: `pom.xml|build.gradle*` → `mvn|gradle`
- Go: `go.mod` → `go build/test`
- Rust: `Cargo.toml` → `cargo`

#### Execution Workflow

1. **Plan**: Produce proposal, run Critical Analyzer
2. **Discover**: Per-repo instruction profiles (precedence rules)
3. **Tests first**: Create/update tests aligned to design
4. **Implement**: Minimal changes, wire DI/config, add docs
5. **Validate**: Build/Lint/Tests, iterate up to 3 times
6. **Summary**: Check in changes and follow-ups

## Part 2: Cross-Repo Architecture Aggregation

See [references/cross-repo-architecture.md](references/cross-repo-architecture.md) for full aggregation rules.

### Quick Reference

#### Required Metadata (frontmatter)

```yaml
---
repo: "<org>/<repo>"
service: "<logical service name>"
domain: "<bounded context/domain>"
owners: ["team@contoso", "@github-handle"]
environment: ["DEV", "PPE", "PROD"]
---
```

#### Endpoint Catalog Schema

Each repo must maintain an "Endpoint Catalog" table:

- Columns: Endpoint ID | Route | Verb | Version | Domain | Dependencies | Owner
- Endpoint ID namespace: `<repoSlug>.<service>.<version>.<resource>.<verb>`

#### Aggregation/Stitching Rules

- Index all `architecture.md` files and extract frontmatter, endpoint catalog, dependencies
- Build a global graph: Nodes = services/endpoints, Edges = dependencies/cross-repo calls
- Flag duplicate Endpoint IDs as errors, divergent contracts as drift

#### Virtual Monorepo Views

- Service dependency map (ASCII) for the open workspace
- Endpoint index across repos (filterable by domain/service)
- Cross-repo risk report (missing owners, missing SLOs, drift/conflicts)

## Coding Guidelines (Generic)

- Prefer async I/O; use cancellation tokens/timeouts
- Centralize configuration (env vars); avoid secrets in code
- Dependency injection where available; keep handlers thin
- Idempotency and retries for external calls; exponential backoff
- Structured logging with correlation IDs

## Observability

- Trace propagation (OpenTelemetry where available)
- Standard fields: CorrelationId, Endpoint/FunctionId, EntityId, Attempt
- Emit metrics at key steps; provide dashboards/queries where relevant

## Quality Gates

- Build: language-appropriate
- Lint/Typecheck: eslint/flake8/golangci-lint/etc.
- Unit tests: fast, isolated; include negative case
- Smoke test: small runtime validation when feasible

## Safety & Compatibility

- Do not change target frameworks or central package versions unless explicitly required
- Follow repo-specific instruction profiles when present (discovery precedence):
  1. `.github/instructions/**/implementation.instructions.md` (domain/team-specific)
  2. `.github/instructions/implementation.instructions.md` (repo default)
  3. Fallback generic guidelines
- No secrets or PII in code or logs

## Part 3: Monorepo Architecture Orchestration

When orchestrating architecture across multiple repos in a single workspace:

### Interaction Flow

1. **Discovery**: List repositories and their key components (APIs, services, data stores)
2. **Per-Repo Docs**: For each repo, document endpoints and services in that repo's `ARCHITECTURE.md`
3. **Monorepo Summary**: After per-repo docs exist, generate a top-level `MONOREPO_ARCHITECTURE.md` that:
   - Consolidates each repo's sections
   - Identifies shared modules, patterns, and dependencies
   - Highlights opportunities for code sharing, consistency, or refactoring
4. **Critical Analysis Gate**: Before generating or appending any architecture content, write a short Proposal (what/why/where) and run critical analysis; incorporate prioritized action items

### Per-Repo `ARCHITECTURE.md` Generation

- Ask one repo and one component at a time
- Confirm complete per-repo documentation before generating the summary
- Use clear headings, bullet lists, and diagrams (PlantUML or ASCII)

### Monorepo Implementation Workflow

When implementing designs across repos:

1. **Discover** design sources: `MONOREPO_ARCHITECTURE.md`, `**/docs/design/*.md`
2. **Detect** frameworks per repo (see Part 1 framework detection)
3. **Produce** an implementation plan: contract per service, edge cases, file map
4. **Execute** changes safely: tests first, then code, then docs
5. **Validate** with quality gates: Build, Lint, Tests
6. **Checkpoint** after 3-5 edits: what changed, results, what's next

### Editing & Safety Rules

- Create the smallest set of changes to meet design intent
- Do not change target frameworks or central package versions unless explicitly stated
- Never commit secrets; use env variables/Key Vault
- Follow the discovered per-repo instruction profile strictly
