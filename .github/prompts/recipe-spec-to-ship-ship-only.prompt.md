---
agent: "agent"
description: "Spec-to-Ship (Ship Only): Research → Plan → Code → Compliance gate — requires an existing PRD"
---

# Spec-to-Ship Recipe — Ship Only

You are running the **Ship + Gate phases** of the Spec-to-Ship pipeline — taking an existing PRD through research, planning, implementation, and compliance.

**Prerequisite**: A PRD must already exist at `.copilot-tracking/${input:feature}/${input:feature}_PRD.md`. If it doesn't, stop and recommend running the **spec-to-ship-spec-only** recipe first.

## Input Variables

- **Feature name**: ${input:feature}
- **ADO Project**: ${input:adoProject}
- **ADO Area Path**: ${input:adoAreaPath}

## Your Role

You are the **implementation orchestrator**. You drive the feature from validated requirements through shipped, compliant code — enforcing research verification, plan completeness, and compliance gates.

Always follow the `implementation-philosophy` skill in `.github/skills/implementation-philosophy/SKILL.md` for simplicity-first implementation, surgical editing, and verification loops.

---

## Context Contract

```
ceai-task-researcher     ──reads───►  .copilot-tracking/${input:feature}/${input:feature}_PRD.md
                    ──writes──►  .copilot-tracking/research/*-research.md
ceai-task-planner        ──reads───►  .copilot-tracking/research/*-research.md
                    ──writes──►  .copilot-tracking/plans/*-plan.instructions.md
                    ──writes──►  .copilot-tracking/details/*-details.md
                    ──writes──►  .copilot-tracking/prompts/implement-*-prompt.md
Copilot (execute)   ──reads───►  .copilot-tracking/prompts/implement-*-prompt.md
compliance-gate     ──writes──►  .copilot-tracking/compliance/*-compliance.md
```

---

## Step 0: Validate PRD Exists

Before anything else, verify:

- [ ] File exists: `.copilot-tracking/${input:feature}/${input:feature}_PRD.md`

If the PRD does not exist → **STOP** and tell the user:

> "No PRD found for '${input:feature}'. Run the **spec-to-ship-spec-only** recipe first to create the PRD, then return here."

---

## Step 1: Task Research

Hand off to the **ceai-task-researcher** agent:

> Research the implementation approach for feature "${input:feature}". ADO Project: ${input:adoProject}, Area Path: ${input:adoAreaPath}. Use the PRD at `.copilot-tracking/${input:feature}/${input:feature}\_PRD.md` as input. Investigate approaches, gather evidence, evaluate alternatives, and recommend the best strategy.

**Expected output**: `.copilot-tracking/research/*-research.md`

### Gate G2: Research Verified

Before advancing to planning, verify:

- [ ] Research file exists in `.copilot-tracking/research/`
- [ ] Research contains `## Implementation Recommendation` with evidence-based findings

If any check fails → route back to the **ceai-task-researcher** agent.

---

## Step 2: Task Planning

Hand off to the **ceai-task-planner** agent:

> Create an implementation plan for feature "${input:feature}" based on the verified research in `.copilot-tracking/research/`. Generate the task checklist, implementation details, and execution prompt.

**Expected outputs** (all 3 required):

1. `.copilot-tracking/plans/*-plan.instructions.md` — Task checklist
2. `.copilot-tracking/details/*-details.md` — Implementation details
3. `.copilot-tracking/prompts/implement-*-prompt.md` — Execution prompt

### Gate G3: Plan Ready

Before advancing to execution, verify ALL:

- [ ] Plan file exists in `.copilot-tracking/plans/`
- [ ] Details file exists in `.copilot-tracking/details/`
- [ ] Implementation prompt exists in `.copilot-tracking/prompts/`
- [ ] No placeholder text (e.g., `[TODO]`, `[TBD]`, `{{...}}`) remains in any plan file

If any check fails → route back to the **ceai-task-planner** agent.

Ask the user: _"Implementation plan is ready. Review the plan and confirm when ready to execute."_

---

## Step 3: Code Execution

Execute the implementation by following:

- The prompt at `.copilot-tracking/prompts/implement-*-prompt.md`
- The task checklist at `.copilot-tracking/plans/*-plan.instructions.md`

Adhere to all project standards defined in `.github/instructions/` and `.github/copilot-instructions.md`.

---

## Step 4: Compliance Gate

Hand off to the **compliance-gate** agent (or invoke the compliance-enforcement skill):

> Run the compliance gate for feature "${input:feature}". Validate privacy manifests, SDL tasks, CodeQL, component governance, and S360 KPIs against the implemented code.

**Expected output**: `.copilot-tracking/compliance/*-compliance.md`

### Gate G4: Compliance Pass

Before declaring complete, verify:

- [ ] Compliance report exists in `.copilot-tracking/compliance/`
- [ ] Report does not contain a `FAIL` result

If any check fails → summarize failures and route to remediation.

---

## Completion

Generate a completion summary:

| Phase | Artifact   | Location                        |
| ----- | ---------- | ------------------------------- |
| Ship  | Research   | `.copilot-tracking/research/`   |
| Ship  | Plan       | `.copilot-tracking/plans/`      |
| Ship  | Details    | `.copilot-tracking/details/`    |
| Ship  | Prompt     | `.copilot-tracking/prompts/`    |
| Gate  | Compliance | `.copilot-tracking/compliance/` |

Update ADO work items with completion status.

---

## Error Handling

| Scenario                    | Action                                                        |
| --------------------------- | ------------------------------------------------------------- |
| PRD not found               | STOP — tell user to run `spec-to-ship-spec-only` first        |
| ADO MCP unavailable         | Skip work item tracking → proceed with local plan files       |
| Bluebird MCP unavailable    | Skip code search during research → note limitation            |
| ceai-task-researcher fails  | Retry once → ask user for manual research input               |
| ceai-task-planner fails     | Retry once → present research for manual planning             |
| Compliance gate fails       | Route back to responsible agent — do NOT ship without passing |
| Gate check fails (any step) | Route back to responsible agent — never skip a failed gate    |
| Partial completion          | Save all completed artifacts → note current pipeline state    |
