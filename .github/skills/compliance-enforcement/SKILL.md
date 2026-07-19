---
name: compliance-enforcement
description: "Automated enforcement of Microsoft's S360 KPIs across privacy, security, and compliance domains. Multi-step compliance review with privacy manifests, SDL tasks, threat models, CodeQL, and component governance. USE FOR: compliance review, privacy manifest, SDL compliance, threat model, CodeQL issues, component governance, S360 KPIs, security review, privacy tagging, telemetry compliance, DFD generation, data flow diagram, vulnerable packages, deprecated packages. DO NOT USE FOR: coding standards enforcement (use language-specific instructions), architecture design (use architecture-design skill), deployment execution (use microsoft-engineering skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# Compliance Enforcement Skill

This skill defines how GitHub Copilot and SWE Agents enforce automated validation of privacy, security, and compliance across the repo. It aligns with **Microsoft's S360 KPIs**, **MCP server integration**, and **agentic workflows**.

## When to Activate

- "compliance review"
- "check privacy manifest"
- "SDL compliance"
- "threat model update"
- "CodeQL issues"
- "component governance scan"
- "S360 validation"
- "security and privacy review"
- PR validation and code review for compliance
- New feature code generation requiring compliance checks

## When NOT to Use

- Enforcing coding standards for a specific language (use language instructions)
- High-level architecture design (use `architecture-design` skill)
- Deploying to Azure/OneBranch (use `microsoft-engineering` skill)
- Cross-repo stitching (use `cross-repo-development` skill)

## Definitions

- **S360 KPIs** → Microsoft's compliance metrics for privacy, security, and telemetry
- **MCP Server** → Microsoft Compliance Platform server used for validation
- **privacy-manifest.json** → Manifest listing telemetry events and their compliance metadata
- **SDL** → Security Development Lifecycle tasks required for secure engineering
- **CodeQL** → Static analysis engine for detecting security vulnerabilities
- **Component Governance (CG)** → Validation of OSS licenses and vulnerable packages
- **Threat Model** → Documented analysis of security risks and mitigations

## Responsibilities

Agents and Copilot must:

1. Validate against component governance rules (see [references/cg-policy.md](references/cg-policy.md))
2. Ensure privacy manifest updates and DFD generation (see [references/privacy-policy.md](references/privacy-policy.md))
3. Ensure SDL compliance and update threat models (see [references/sdl-policy.md](references/sdl-policy.md))
4. Ensure CodeQL vulnerabilities are identified and remediated
5. Verify S360 KPIs show no unresolved compliance issues
6. Identify and mitigate transitive risks in dependencies

## Requirements & Constraints

| ID      | Type        | Description                                                                                    |
| ------- | ----------- | ---------------------------------------------------------------------------------------------- |
| REQ-001 | Privacy     | All telemetry must be mapped in `privacy-manifest.json`                                        |
| REQ-002 | Privacy     | All telemetry/logging must include privacy attributes (Category, RetentionDays, Justification) |
| THR-001 | ThreatModel | Threat model must be generated/updated when new risks arise                                    |
| BUG-001 | CodeQL      | All CodeQL issues must be fixed during code generation/review                                  |
| CG-001  | Governance  | Component Governance issues must be remediated                                                 |
| CG-002  | Governance  | Deprecated/flagged packages must be auto-removed or replaced                                   |
| CG-003  | Governance  | PR reviews must leave inline comments for flagged/vulnerable packages                          |
| SEC-002 | Security    | No hardcoded secrets or insecure transport allowed                                             |
| SEC-003 | Security    | Agents must cite official MS guidance when recommending fixes                                  |
| LIC-001 | Compliance  | All dependencies must comply with approved OSS licenses                                        |
| PAT-001 | Pattern     | Use structured logging with privacy annotations                                                |
| GUD-001 | Guideline   | Agents must suggest manifest updates for unmapped telemetry                                    |

## Acceptance Criteria

- **AC-001:** Copilot must enforce compliance during code generation
- **AC-008:** Copilot/SWE Agent must enforce compliance during PR validation
- **AC-009:** Agents must suggest secure/license-compliant alternatives for deprecated packages
- **AC-011:** PR comments must reference Microsoft S360 guidance when remediation is needed
- **AC-012:** Code generation and reviews must enforce left-shift & secure-by-design principles
- **AC-013:** PR comments must highlight deprecated packages and suggest alternatives
- **AC-014:** PR comments must highlight risky code patterns (CodeQL issues) and suggest compliant fixes

## Compliance Workflow

### During Code Generation

1. Generate inline mitigation stubs
2. Propose secure-by-design alternatives
3. Annotate telemetry with privacy attributes
4. Suggest privacy-manifest.json updates for new events
5. Prevent insecure patterns

### During Code Review / PR Validation

1. Validate privacy/security/compliance
2. Add inline remediation guidance with S360 references
3. Validate mitigations are complete, implemented, documented
4. Flag missing threat model updates
5. Block non-compliant code before merge

## Key Enhancements for Left Shift

- **Repo-wide scanning**: Scan full repo for privacy, security, compliance, CodeQL, CG, SDL, and threat model gaps
- **Reference official flagged package lists**: Use [aka.ms/componentgovernance](https://aka.ms/componentgovernance) and [aka.ms/s360](https://aka.ms/s360)
- **Shift left**: All compliance checks must run before PR merge
- **Actionable remediation**: PRs must block until issues are fixed with actionable guidance
- **Embed security/privacy into dev**: Threat models, SDL, and manifests must be updated during dev, not post-release

## Output Format

When reporting, agents must include:

1. **Issue detected** (bug, policy violation, gap)
2. **Reference** to related policy (privacy, SDL, CG)
3. **Recommended fix** with official guidance reference

Agents must **always suggest corrective action** — not just flag issues.

## Reference Documents

For detailed policy rules, see:

- [Privacy Policy](references/privacy-policy.md) — Privacy-by-design enforcement, telemetry tagging, DFD generation
- [SDL Policy](references/sdl-policy.md) — STRIDE threat modeling, CodeQL prevention, SDL tasks
- [CG Policy](references/cg-policy.md) — Deprecated packages, vulnerable dependencies, transitive risks

For operational enforcement procedures, see:

- [Code Generation Enforcement](references/codegen-enforcement.md) — Rules, templates, and enforcement steps during code generation
- [Code Review Enforcement](references/review-enforcement.md) — PR validation, inline comment templates, accessibility review

## External References

- [S360 Portal](https://s360.microsoft.com/)
- [CodeQL](https://aka.ms/codeql)
- [Component Governance](https://aka.ms/componentgovernance)
- [SDL Guidance](https://aka.ms/sdl)
- [Threat Modeling](https://aka.ms/threatmodel)
- [Privacy](https://aka.ms/privacy)
- [Security](https://aka.ms/security)
