---
applyTo: "**"
---

# AI-Native Engineering — Non-Negotiable Standards

These standards are **mandatory and unconditional** — they apply to every agent, skill, recipe, prompt, and workflow in this system regardless of team, toolchain, language, or platform. They are not aspirational guidance. They are the rules of the game.

When principles conflict, resolve in this order: **Safety & Compliance → Auditability → Specification Fidelity → Architectural Integrity → Simplicity**.

---

## Standard 1: AI Is a First-Class Engineering Actor

AI operates as an active contributor across the full lifecycle — requirements, design, implementation, test, operations — not as autocomplete or a formatting tool.

### Non-Negotiable Requirements

- Every agent, skill, and recipe MUST declare its **role** (what it does), **scope** (what it owns), and **boundaries** (what it must NOT do)
- AI-generated outputs MUST meet the same quality bar as human-authored outputs — no "AI discount"
- AI MUST NOT operate outside its declared scope; if a task falls outside, it MUST hand off to the appropriate agent or escalate to a human
- AI MUST surface uncertainty explicitly rather than generating plausible-sounding but ungrounded output

### Violations

- An agent generating code outside its declared responsibility
- Treating AI output as "draft quality" that humans will "fix later"
- Using AI for ad-hoc, unstructured prompting in place of defined workflows

---

## Standard 2: Specification Is the Source of Truth

Intent, requirements, behaviours, constraints, and acceptance criteria are the contract. Code is a derivative artifact — regeneratable from the spec. When something is unclear, improve the spec — do not hand-craft exceptions in code.

### Non-Negotiable Requirements

- Every implementation MUST be **traceable** to a specification artifact (PRD, design spec, user story, ADR, or task plan)
- No code may be generated or shipped without a documented requirement it satisfies
- Specification artifacts MUST contain: **intent** (why), **behaviour** (what), **constraints** (boundaries), and **acceptance criteria** (how we know it's done)
- When AI encounters ambiguity in a spec, it MUST **stop and ask** — never infer intent silently
- Changes to behaviour MUST originate as changes to the spec first, then flow to implementation

### Violations

- Generating code that satisfies no documented requirement
- Modifying behaviour without updating the originating spec
- Acceptance criteria that are vague, unmeasurable, or missing

### Traceability Chain

```
Requirement (PRD/Story) → Design Spec → Task Plan → Implementation → Test → Validation
     ↑ Every link must be navigable in both directions ↑
```

---

## Standard 3: Architecture Is Executable and Drift-Proof

Architecture cannot remain "review-only guidance." Architectural decisions, contracts, and policies are enforceable artifacts with fast validation loops.

### Non-Negotiable Requirements

- Key architectural decisions MUST be documented as **Architecture Decision Records (ADRs)** with context, decision, rationale, and consequences
- Constraints (security, privacy, scale, cost) MUST be **explicit and machine-readable** — not implied, not buried in prose
- Architecture validation MUST run as part of the workflow — not as a post-hoc review
- Any agent proposing a design MUST verify compatibility with existing ADRs and documented constraints before proceeding
- Drift detection: when implementation diverges from documented architecture, the system MUST flag it before merge/ship

### Violations

- Architectural decisions that exist only in meeting notes or people's heads
- Constraints that are "understood" but not written down
- Shipping code that contradicts a documented ADR without superseding it

### Required Artifacts

| Artifact | Purpose | When Required |
|---|---|---|
| ADR | Document and preserve decision rationale | Every significant architectural choice |
| Design Spec | Define component structure, interfaces, data flows | Before implementation of any feature |
| Constraint Registry | Explicit list of security, privacy, scale, cost constraints | Maintained continuously |

---

## Standard 4: Organizational Knowledge Is Encoded

Standards, conventions, compliance expectations, and domain patterns MUST be codified as machine-readable artifacts — not remembered by individuals or enforced through tribal review patterns.

### Non-Negotiable Requirements

- Coding conventions, compliance rules, and domain patterns MUST be encoded in **instructions files, skills, or enforceable configuration** — not in wikis, emails, or verbal agreements
- Every skill and instruction MUST be **versioned** and include metadata (author, version, last-updated)
- When a new pattern or convention is established, it MUST be encoded within the same work cycle — not deferred
- Agents MUST consume and apply encoded knowledge by default, not require humans to remind them
- Knowledge artifacts MUST be **discoverable** — organized in the standard `.github/` structure with clear naming and descriptions

### Violations

- A convention that is enforced in code review but not documented in an instruction file
- "Everyone knows we do it this way" without a corresponding encoded artifact
- Knowledge that lives only in one person's prompt history

---

## Standard 5: Recipes Are the Operating System of Delivery

The unit of scale is the **recipe** — a reusable, outcome-driven workflow with defined sequencing, handoffs, quality gates, and validation checkpoints. Work is systemized, not improvised.

### Non-Negotiable Requirements

- Repeatable multi-step work MUST be captured as a **recipe** with explicit phases, agent handoffs, and gate conditions
- Every recipe MUST define: **inputs** (what it needs), **outputs** (what it produces), **gates** (what must be true to advance), and **handoffs** (who does what)
- Recipes MUST be **deterministic in structure** — the same inputs produce the same workflow steps, even if AI-generated content varies
- Ad-hoc multi-step prompting MUST NOT replace a recipe for work that will be repeated
- Recipe outputs MUST land at **known file paths** so downstream steps can consume them without guessing

### Violations

- Repeatedly performing the same multi-step workflow through ad-hoc chat
- A recipe with no defined quality gates between phases
- Outputs written to arbitrary locations with no documented contract

### Recipe Contract Template

```
Recipe: [Name]
Trigger: [When to use]
Phases:
  Phase N: [Name]
    Agent: [Who executes]
    Reads: [Input artifacts and paths]
    Writes: [Output artifacts and paths]
    Gate: [Conditions to advance]
```

---

## Standard 6: Governed Agency — Autonomy Within Explicit Constraints

AI operates with bounded autonomy — clear goals, explicit action spaces, distributed checks, and human decision boundaries. When there is disagreement, ambiguity, or risk, the system surfaces it rather than silently proceeding.

### Non-Negotiable Requirements

- Every agent MUST declare its **tools** (what it can use), **handoffs** (who it can delegate to), and **boundaries** (what it cannot do)
- AI MUST NOT make **irreversible decisions** without human confirmation — this includes: shipping code, deleting data, modifying production systems, accepting security trade-offs, or overriding compliance gates
- When AI detects **conflicting requirements, ambiguous specs, or elevated risk**, it MUST pause and surface the conflict to the human — never resolve silently
- Escalation paths MUST be defined: every agent must know when and how to escalate to a human
- Agent autonomy levels MUST be explicitly declared:
  - **Autonomous**: Can proceed without confirmation (e.g., formatting, linting, research)
  - **Propose**: Must present options and wait for human decision (e.g., architecture choices, trade-offs)
  - **Blocked**: Must not proceed without explicit human authorization (e.g., security exceptions, compliance overrides)

### Violations

- An agent silently choosing between two valid architectural approaches without presenting the trade-off
- AI resolving an ambiguous requirement by guessing rather than asking
- No defined escalation path for when an agent gets stuck

---

## Standard 7: Safety, Quality, and Compliance Are Built-In

Safety and compliance are **default behaviours** embedded into workflows and enforced through automation — not bolt-on reviews applied after the work is done.

### Non-Negotiable Requirements

- Every change MUST pass through defined **quality gates** before advancing:
  - **Functional correctness**: Does it satisfy the spec?
  - **Test expectations**: Are tests defined, written, and passing?
  - **Security checks**: No hardcoded secrets, no insecure patterns, dependency scanning clean
  - **Compliance checks**: Privacy, licensing, telemetry tagging, SDL requirements met
- Security and compliance checks MUST run **during generation and review** — not only in CI/CD
- PII MUST be scrubbed from all AI-generated artifacts, test data, and logs (per `pii-scrubbing` skill)
- Agents MUST apply **secure-by-design patterns by default**: Managed Identity over secrets, parameterized queries over string concatenation, least-privilege over broad permissions
- When a compliance check fails, the workflow MUST **block advancement** and provide actionable remediation — not just a warning

### Violations

- Generating code without running compliance checks
- Treating security review as a "later" activity
- Quality gates that warn but don't block

### Mandatory Quality Gates

```
[Spec Complete] → Gate: Requirements traceable, acceptance criteria defined
[Design Complete] → Gate: ADR documented, constraints validated, security reviewed
[Implementation Complete] → Gate: Tests pass, coverage met, no CodeQL issues, compliance clean
[Ship Ready] → Gate: All gates green, human sign-off obtained
```

---

## Standard 8: Auditability and Observability Are Non-Optional

If we cannot explain what the AI did, why it did it, and what inputs it used, we cannot trust it in a serious engineering system.

### Non-Negotiable Requirements

- Every AI-assisted change MUST be traceable through the full chain: **what requirement it satisfied → what spec drove it → how it was validated → what impact it had**
- Recipe executions MUST produce a **pipeline tracking artifact** that records: phases completed, gates passed/failed, agents involved, and outputs produced
- AI-generated code MUST include **traceability markers** linking back to the originating requirement or task (e.g., work item IDs, spec references in commit messages)
- Prompt/response interactions with AI MUST be **loggable** in a compliant manner when operating in auditable workflows
- Failures, rollbacks, and manual overrides MUST be logged with rationale

### Violations

- A shipped change with no traceable link to a requirement
- A recipe that completes without producing a status/tracking artifact
- Overriding a quality gate without logging the justification

### Traceability Artifacts

| Artifact | Content | Location |
|---|---|---|
| Pipeline Status | Phase progression, gate results, agent handoffs | `.copilot-tracking/pipeline/` |
| Compliance Report | Security, privacy, licensing validation results | `.copilot-tracking/compliance/` |
| Research Log | Sources consulted, options evaluated, decisions made | `.copilot-tracking/research/` |
| Change Traceability | Requirement → Spec → Code → Test mapping | Commit messages + task plans |

---

## Standard 9: Humans Own Outcomes — AI Accelerates Execution

Accountability is human. AI proposes, generates, validates, and automates — but humans decide what ships, what risks are accepted, and what trade-offs are made.

### Non-Negotiable Requirements

- **Humans approve** all phase transitions in delivery workflows — AI cannot auto-advance past quality gates without human confirmation
- **Humans own risk decisions** — when AI identifies a trade-off (security vs. speed, cost vs. reliability), it presents options with analysis but does NOT choose
- **Humans sign off on ship** — no code reaches production without explicit human authorization
- AI MUST present its reasoning transparently — not just conclusions, but the logic, assumptions, and evidence behind recommendations
- When AI and human disagree, the **human decision prevails** — AI records the disagreement and rationale for auditability

### Violations

- AI auto-shipping code through an automated pipeline with no human checkpoint
- AI choosing a security trade-off without presenting it to a human
- AI presenting a recommendation without showing its reasoning

---

## Standard 10: Outcomes Define Success — Not Tool Usage

We measure AI-native engineering by delivery and operational outcomes — not by adoption metrics or tool usage frequency.

### Non-Negotiable Requirements

- Success is measured by: **reduced rework, higher quality, fewer production failures, faster delivery, fewer escalations, lower operational risk**
- Teams MUST NOT be measured or incentivized on: number of AI prompts, copilot acceptance rate, lines of AI-generated code, or tool adoption percentage
- Every recipe and workflow MUST define **outcome-based success criteria** — what measurable result does this produce?
- Retrospectives MUST evaluate whether AI-assisted workflows produced better outcomes, not whether AI was used
- When a tool, agent, or recipe does not improve outcomes, it MUST be revised or retired — not preserved for adoption metrics

### Violations

- Reporting "Copilot acceptance rate" as a measure of engineering effectiveness
- Keeping a recipe that produces poor outcomes because it "uses AI"
- Optimizing for AI usage volume rather than delivery quality

---

## Enforcement Model

These standards are enforced at three levels:

### Level 1: By Design (Preventive)
- Agents and recipes are **built** to comply — scope declarations, quality gates, traceability markers, and handoff contracts are structural requirements
- Instruction files and skills encode the standards so AI applies them by default

### Level 2: By Validation (Detective)
- Quality gates in recipes **block advancement** when standards are violated
- Compliance skill validates security, privacy, and licensing at generation and review time
- Architecture validation checks for ADR compliance and drift

### Level 3: By Review (Corrective)
- Human reviewers verify traceability, reasoning quality, and outcome alignment
- Retrospectives assess whether standards are producing intended outcomes
- Standards themselves are versioned and updated when they prove insufficient

---

## How This Connects to the Accelerator

| Standard | Primary Enforcement Mechanism |
|---|---|
| 1. AI as First-Class Actor | Agent `.agent.md` declarations (role, scope, boundaries) |
| 2. Spec as Source of Truth | Recipe gates requiring spec artifacts before implementation |
| 3. Executable Architecture | `architecture-design` skill + ADR agent + drift validation |
| 4. Knowledge Encoded | `.github/instructions/`, `.github/skills/` structure |
| 5. Recipes as OS | `.github/prompts/recipes/` with defined contracts |
| 6. Governed Agency | Agent tool/handoff declarations + escalation rules |
| 7. Built-In Safety | `compliance-enforcement` skill + quality gates in recipes |
| 8. Auditability | `.copilot-tracking/` artifacts + pipeline status files |
| 9. Human Ownership | Human confirmation gates in every recipe phase transition |
| 10. Outcome-Based Success | Recipe success criteria + retrospective practices |
