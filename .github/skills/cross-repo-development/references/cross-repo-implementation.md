# Cross-Repo Implementation Workflow

These instructions guide automated and human implementers to turn a design document into working code across multiple repositories in one workspace.

## Discovery & Design Intake

- Inputs: `MONOREPO_ARCHITECTURE.md`, `**/docs/design/*.md`, `**/docs/architecture/*.md`
- Extract:
  - Services/endpoints/functions to add/modify
  - Data contracts and configuration
  - Dependencies (DB, messaging, search)
  - Observability and SLO expectations

## Framework/Build Detection

Detect frameworks and tools by presence of files:
- .NET: `*.sln`, `*.csproj` → `dotnet` build/test
- Node.js: `package.json` → `npm|yarn|pnpm`
- Python: `pyproject.toml|requirements.txt` → `pip|uv|poetry`
- Java: `pom.xml|build.gradle*` → `mvn|gradle`
- Go: `go.mod` → `go build/test`
- Rust: `Cargo.toml` → `cargo`
- Azure Functions: `host.json`/`function.json`
- Containerization: `Dockerfile*`

Choose the native toolchain per repo; do not assume a single framework.

## Implementation Plan (per service)

- **Contract** (2–4 bullets): inputs/outputs, data shapes, error modes, success criteria
- **Edge cases** (3–5): null/empty, large input, auth, timeouts, concurrency
- **File map**: list files to edit/create; smallest diffs; preserve style
- **Testing**: add/update minimal unit tests first (happy path + 1–2 edges)

## Coding Guidelines (generic)

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
- Lint/Typecheck: eslint/flake8/golangci-lint/etc. when available
- Unit tests: fast, isolated; include negative case
- Smoke test: small runtime validation when feasible

## Safety & Compatibility

- Do not change target frameworks or central package versions unless explicitly required; otherwise propose separately
- Follow repo-specific implementation instruction profiles when present (discovery precedence):
  1. `.github/instructions/**/implementation.instructions.md` (domain/team-specific)
  2. `.github/instructions/implementation.instructions.md` (repo default)
  3. Fallback generic guidelines
- No secrets or PII in code or logs

## Execution Workflow

1. **Plan**: produce a brief proposal (what/why/where) and run the Critical Analyzer; incorporate its action items
2. **Discover** per-repo instruction profiles (per precedence above) and summarize effective rules before coding
3. **Tests first**: create/update tests aligned to the design and per-repo rules
4. **Implement**: apply minimal changes; wire DI/config; add docs following per-repo rules
5. **Validate**: run Build/Lint/Tests; iterate up to 3 times on failures and document outcomes
6. **Check in** a summary of changes and follow-ups

## Troubleshooting

- Build tool not found: detect and install instructions; fall back to containerized build if available
- Flaky tests: retry with backoff; isolate external dependencies
- Environment variables missing: document required keys and defaults
