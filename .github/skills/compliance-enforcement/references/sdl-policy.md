# SDL Threat Model Policy

This file defines **common SDL compliance tasks & Threat Modeling Policy**.
Agents must use this knowledge to enforce **SDL compliance** during code generation and code review.

## Purpose
Enable **shift-left enforcement** of SDL principles by embedding threat modeling into daily development activities.

## SDL Enforcement

### Data Protection
- Validate encryption at rest and in transit.
- Enforce certificate validation and secure transport.

### Identity & Access
- Use secure authentication and authorization patterns.
- Avoid hardcoded secrets or credentials.

### Threat Modeling
- Update threat model documentation when new features introduce risk.
- Ensure diagrams and mitigations reflect the **current architecture**.

### Developer Workflow
- Surface SDL gaps and suggest mitigations early in the coding lifecycle.
- Prevent merge of insecure code or designs.

## Rules

- Apply STRIDE categories (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege).
- For each identified threat:
  - Mitigation design must exist before code generation.
  - Code-level enforcement must exist during generation.
- Reject code that introduces unmitigated STRIDE-class threats.
- Generate a new threat model if none exists.
- Update existing threat models when new features, APIs, or services are added.

## Example DFD (Mermaid)

```mermaid
graph TD
  User[End User] -->|Input [Spoofing Risk]| UI[Web UI]
  UI -->|REST/GraphQL [Tampering Risk]| API[Backend API Service]
  API -->|Reads/Writes [Info Disclosure Risk]| DB[(SQL Database)]
  API -->|Publishes Events [DoS Risk]| EventHub[(Event Hub)]
  API -->|Calls [Elevation of Privilege Risk]| External[External SaaS API]
```

## CodeQL Bug Prevention

Avoid patterns that trigger CodeQL alerts:
- Unsafe deserialization
- SQL injection
- Hardcoded secrets

Use:
- Parameterized queries
- Strong input validation
- Secure exception handling and logging

## Acceptance Criteria

- **AC-001:** Given a new telemetry event, when it is added to code, then it must be mapped in `privacy-manifest.json`.
- **AC-002:** Given a repo change, when new endpoints or data stores are introduced, then agents must update `docs/threatmodel.md`.
- **AC-003:** Given a new feature, when it introduces risk, and agents are asked for a review, then `docs/dfd.md` must reflect the new flows.
- **AC-014:** Given a PR with risky code patterns (CodeQL issues), when agents review, then they must highlight the exact code and suggest an alternative that satisfies S360 KPIs.
- **AC-006:** Given a release, when SDL validation runs, then all required tasks must be completed.
