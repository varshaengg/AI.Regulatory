---
name: ai-native-standards-enforcement
description: "Validates compliance with AI-Native Engineering non-negotiable standards. Checks traceability, specification completeness, quality gates, auditability, and governed agency requirements. USE FOR: standards compliance check, validate traceability, check quality gates, audit readiness, verify spec completeness, governed agency review, pre-ship validation, standards review. DO NOT USE FOR: specific language coding standards (use language instructions), S360/privacy/security compliance (use compliance-enforcement skill), architecture design (use architecture-design skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# AI-Native Standards Enforcement Skill

This skill validates that work products comply with the AI-Native Engineering non-negotiable standards defined in `.github/instructions/ai-native-standards.instructions.md`. It operates as a **checkpoint validator** — invoked at quality gates, before phase transitions, and during reviews.

## When to Activate

- "check standards compliance"
- "validate against AI-native standards"
- "is this ready to advance?"
- "audit readiness check"
- "traceability review"
- "quality gate validation"
- Before any recipe phase transition
- During PR review for standards alignment
- Pre-ship readiness assessment

## When NOT to Use

- S360/privacy/security-specific compliance (use `compliance-enforcement` skill)
- Language-specific coding standards (use language instructions)
- Architecture design work (use `architecture-design` skill)
- Deployment execution (use `microsoft-engineering` skill)

## Validation Checklists

### Checkpoint A: Specification Readiness (Standard 2)

Before implementation can begin, verify:

- [ ] **Requirement exists**: A documented requirement (PRD, user story, or task) drives this work
- [ ] **Intent is stated**: The "why" is explicit — not just "what" to build
- [ ] **Behaviour is defined**: Expected inputs, outputs, and interactions are specified
- [ ] **Constraints are explicit**: Security, privacy, performance, and cost boundaries are documented
- [ ] **Acceptance criteria are measurable**: Each criterion can be objectively verified as pass/fail
- [ ] **Traceability link exists**: The spec references the originating requirement

**Gate rule**: If any item is missing, implementation MUST NOT begin. Return to the spec author for remediation.

### Checkpoint B: Design Readiness (Standards 3, 4)

Before coding can begin, verify:

- [ ] **ADR documented**: If an architectural decision was made, an ADR exists with context, decision, rationale, and consequences
- [ ] **Constraints validated**: Design respects all constraints in the constraint registry
- [ ] **No ADR conflicts**: Design does not contradict existing ADRs without a superseding ADR
- [ ] **Knowledge encoded**: Any new conventions or patterns are captured in instruction files or skills — not left as tribal knowledge
- [ ] **Security reviewed**: Design has been evaluated for security implications

**Gate rule**: If ADR is missing for a significant decision or design contradicts existing constraints, design phase MUST be revisited.

### Checkpoint C: Implementation Readiness (Standards 5, 7, 8)

Before code can be submitted for review, verify:

- [ ] **Spec traceability**: Every code change traces back to a documented requirement
- [ ] **Tests exist**: Tests are written that validate the acceptance criteria
- [ ] **Tests pass**: All tests execute successfully
- [ ] **Coverage met**: Test coverage meets the project minimum
- [ ] **Security clean**: No hardcoded secrets, no insecure patterns, dependency scan clean
- [ ] **Compliance clean**: Privacy, licensing, and SDL requirements satisfied
- [ ] **PII scrubbed**: No real PII in generated artifacts, test data, or logs
- [ ] **Tracking artifact updated**: Pipeline status file reflects current state

**Gate rule**: If security or compliance checks fail, advancement is BLOCKED until remediation is complete.

### Checkpoint D: Ship Readiness (Standards 8, 9)

Before code can be shipped, verify:

- [ ] **All gates green**: Checkpoints A, B, and C are satisfied
- [ ] **Human sign-off**: A human has explicitly authorized this change for production
- [ ] **Traceability complete**: Full chain from requirement → spec → code → test → validation is navigable
- [ ] **Compliance report generated**: `.copilot-tracking/compliance/` contains validation results
- [ ] **Rollback plan exists**: A plan for reverting the change is documented if applicable
- [ ] **Outcome criteria defined**: Success metrics for this change are stated and measurable

**Gate rule**: No code reaches production without explicit human authorization and complete traceability.

## Agent Compliance Review

When reviewing an agent definition (`.agent.md`), verify:

- [ ] **Role declared**: What the agent does is clearly stated
- [ ] **Scope declared**: What the agent owns is bounded
- [ ] **Boundaries declared**: What the agent must NOT do is explicit
- [ ] **Tools declared**: The agent's available tools are listed
- [ ] **Handoffs declared**: The agent knows who to hand off to and when
- [ ] **Escalation path exists**: The agent knows when to stop and ask a human

## Recipe Compliance Review

When reviewing a recipe (`.prompt.md`), verify:

- [ ] **Phases defined**: Work is broken into explicit phases
- [ ] **Gates defined**: Conditions to advance between phases are stated
- [ ] **Handoffs explicit**: Which agent does what is declared
- [ ] **Input/output contracts**: Each phase declares what it reads and what it writes
- [ ] **Output paths known**: Outputs land at documented, predictable file paths
- [ ] **Human checkpoints**: Phase transitions require human confirmation
- [ ] **Success criteria defined**: Outcome-based measures of success are stated

## Reporting Format

When reporting compliance status, use this structure:

```markdown
## AI-Native Standards Compliance Report

**Subject**: [What is being validated]
**Checkpoint**: [A | B | C | D | Agent Review | Recipe Review]
**Date**: [timestamp]
**Status**: [PASS | FAIL | PARTIAL]

### Results

| # | Check | Status | Finding |
|---|-------|--------|---------|
| 1 | [Check name] | ✅ PASS / ❌ FAIL / ⚠️ PARTIAL | [Detail] |

### Blocking Issues (must fix)
- [Issue description + remediation action]

### Recommendations (should fix)
- [Issue description + suggested improvement]

### Verdict
[ADVANCE / BLOCK — with rationale]
```

## Integration with Recipes

This skill is invoked at quality gates within recipes. The standard integration pattern:

```
Phase N completes
  → Invoke ai-native-standards-enforcement (Checkpoint X)
  → If PASS: advance to Phase N+1
  → If FAIL: route back to responsible agent with blocking issues
  → If PARTIAL: present findings to human for decision
```

## Relationship to Other Skills

| Concern | This Skill | Other Skill |
|---|---|---|
| Traceability & spec fidelity | ✅ | — |
| Quality gate structure | ✅ | — |
| Agent/recipe structural compliance | ✅ | — |
| S360, privacy, SDL, CodeQL | Delegates → | `compliance-enforcement` |
| Coding conventions | Delegates → | Language instruction files |
| Architecture design quality | Delegates → | `architecture-design` |
| Deployment readiness | Delegates → | `microsoft-engineering` |
