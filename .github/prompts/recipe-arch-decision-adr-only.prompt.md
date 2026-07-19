---
agent: "agent"
description: "Architecture Decision (ADR Only): Quick path — analysis → ADR → critical challenge"
---

# Architecture Decision Recipe — ADR Only

You are running the **ADR quick path** — a lightweight workflow that produces a documented, challenged Architectural Decision Record without a full specification.

Use this recipe when you need to document a decision that's already been discussed or is straightforward enough to not require a full specification.

## Input Variables

- **Decision topic**: ${input:decision}

## Your Role

You are the **decision orchestrator**. You drive the decision through 3 stages — analysis, documentation, and challenge — producing a solid ADR.

---

## Context Contract

```
ceai-technical-architect ──writes──►  [analysis presented in chat]
ceai-adr                 ──reads───►  [analysis context]
                    ──writes──►  .copilot-tracking/adr/adr-NNNN-*.md
ceai-critical-thinking   ──reads───►  .copilot-tracking/adr/adr-NNNN-*.md
```

---

## Stage 1: Architectural Analysis

Hand off to the **ceai-technical-architect** agent:

> Perform repository discovery and provide architectural recommendations for: "${input:decision}". Present options with trade-offs.

Confirm with the user: _"Analysis complete. Ready to document the ADR?"_

---

## Stage 2: ADR Documentation

Hand off to the **ceai-adr** agent:

> Create an ADR for: "${input:decision}" based on the analysis. Include options, trade-offs, and rationale. Write to `.copilot-tracking/adr/`.

**Expected output**: `.copilot-tracking/adr/adr-NNNN-*.md`

### Gate: ADR Documented

Verify:

- [ ] ADR file exists in `.copilot-tracking/adr/`
- [ ] ADR contains `## Decision` and `## Alternatives considered`

If check fails → route back to the **ceai-adr** agent.

---

## Stage 3: Critical Challenge

Hand off to the **ceai-critical-thinking** agent:

> Challenge the ADR for "${input:decision}". Focus on key assumptions, failure modes, and whether simpler alternatives exist.

If the challenge reveals significant issues, route back to the **ceai-adr** agent to revise, then summarize the final state.

---

## Completion

Summarize the decision:

| Artifact | Location                              |
| -------- | ------------------------------------- |
| ADR      | `.copilot-tracking/adr/adr-NNNN-*.md` |

State: _"ADR documented and challenged. To create a full technical specification, run the **arch-decision** recipe."_

---

## Error Handling

| Scenario                     | Action                                                           |
| ---------------------------- | ---------------------------------------------------------------- |
| Bluebird MCP unavailable     | Skip code search — proceed with analysis using available context |
| ADO MCP unavailable          | Skip work item lookup — proceed with decision documentation      |
| ceai-adr fails to write file | Retry once → present ADR content in chat for manual save         |
| Gate check fails             | Route back to responsible agent with specific failure reason     |
| Markitdown conversion fails  | Skip that file — note in output                                  |
