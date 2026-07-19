# Compliance — Code Generation Enforcement

Operational procedures for enforcing secure-by-design, SDL, component governance (CG), and privacy-tagging during code generation.

## Activation

Activate whenever agent is asked to generate or modify code, create new endpoints, add/modify logic, add telemetry, or change data flows.

### Signals Requiring Full Policy Checks
- Creating new public/private API endpoints
- Adding new feature/component/integration
- Adding new telemetry/logging calls
- Introducing new dependencies/packages (NuGet/NPM)
- Adding or changing storage, messaging, or external integrations

## Core Rules for Generated Code

### Secrets & Credentials
- Never emit plaintext secrets or credentials in code
- Use Key Vault / managed identities; scaffold secret retrieval code instead of hardcoding

### Data Access
- Use parameterized queries or ORM safe APIs (no raw SQL concatenation)

### Dependencies
- Prefer approved packages only; avoid deprecated/vulnerable libraries
- Suggest secure alternatives where needed

### Input Validation
- Generate input validation and schema checks (server-side) for any external input

### Logging & Telemetry
- Do not log PII or secrets
- Apply structured logging (`ILogger<T>`) and privacy tags to telemetry events
- Ensure telemetry events are referenced in `privacy-manifest.json`

### Transport & Storage
- Enforce TLS for external calls and encrypt sensitive data at rest

### Error Handling
- Avoid leaking stack traces or sensitive details in responses; provide sanitized errors

## Enforcement Steps

### 1. Pre-generation Analysis
- Inspect repo for policy files (CG, privacy, SDL)
- Load current `docs/threatmodel.md` and `docs/dfd.md` (if present)

### 2. On Generation
For each new file/change:
- Check dependency proposals against CG allowlist/vulnerability lists
- Identify telemetry/logging calls and map to `privacy-manifest.json`
- Identify new endpoints/services/storage and mark DFD/threatmodel delta
- Insert security scaffolding (auth middleware, input validation, parameterized queries)

### 3. Post-generation
- If new telemetry found: produce a suggested `privacy-manifest.json` entry
- If new flows or endpoints: append DFD node(s) and STRIDE entries to `docs/threatmodel.md`
- Run static-safety heuristics (basic CodeQL pattern avoidance)

### 4. Before PR Creation
Create a checklist comment summarizing:
- Dependencies added (and their risk)
- Telemetry added and suggested manifest entries
- DFD/threatmodel changes required
- Security scaffolding added
- If high-risk issues exist, **stop** and produce blocking guidance

## Inline Comment Template

```
Violation: <Policy-Area> (e.g., Component Governance / Privacy / SDL)
Location: <file>:<line>
Issue: <short description>
Policy: <link to policy doc>
Fix: <exact fix or patch snippet>
Evidence: <optional: commands to validate>
```

## Privacy Manifest Suggestion Template

```json
{
  "EventName": "NewEventName",
  "DataCategory": "Pseudonymous",
  "RetentionDays": 30,
  "Justification": "Explain business reason and minimal data required"
}
```

## CodeQL / Pattern Avoidance

Flag potential CodeQL triggers:
- Unsafe deserialization patterns
- Raw SQL concatenation
- Insecure crypto usage (weak algorithms)
- Hardcoded credentials

For each flagged pattern include the unsafe snippet, a secure replacement, and a reference.

## Do / Don't

**Do**:
- Produce secure-by-default scaffolding
- Annotate telemetry with privacy tags
- Suggest explicit manifest and threat model updates
- Provide machine-parseable issue comments with fixes

**Don't**:
- Autofill secrets or credentials
- Introduce unapproved packages without mitigation
- Merge code with unmitigated critical risks
