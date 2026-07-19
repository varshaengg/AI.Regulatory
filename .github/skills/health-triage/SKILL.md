---
name: health-triage
description: >-
  Health triage domain knowledge for the Health Triage Agent. Contains KQL query templates,
  triage decision trees, SQL assessment rubrics, S360 compliance patterns, and remediation templates.
  USE FOR: health triage scans, Application Insights KQL queries, request failure triage,
  dependency failure classification, performance/SLA analysis, SQL health assessment,
  S360 compliance scanning, Bug/IcM draft generation, health briefing formatting,
  work item deduplication, auto-fix pipeline delegation, team onboarding,
  first-time setup, team config YAML generation.
  DO NOT USE FOR: general Azure observability (use azure-observability skill),
  incident response or IcM management (use support-agent),
  code research and planning (use task-researcher/task-planner agents),
  wisdom mining (use wisdom-miner agent).
---

# Health Triage Skill Library

## When to Activate

- "scan last 24 hours", "health scan", "s360 scan", "full scan"
- "ui perf last 24 hours", "ui session {id}"
- Working with `.github/health-triage/` files
- Service health monitoring and incident triage workflows
- S360 compliance posture assessment

## When NOT to Use

- General Azure observability dashboards (use `azure-observability` skill)
- IcM incident enrichment and TSG execution (use `support-agent`)
- Code research and implementation planning (use `task-researcher` / `task-planner`)
- Wisdom mining from reports (use `wisdom-miner` agent)

This skill library provides domain-specific knowledge for the Health Triage Agent. Each file covers a distinct phase of the health triage pipeline and is loaded on demand by the agent orchestrator.

## Skill Files

| File                                                 | Phase(s)                    | Description                                                                                                                                                                           |
| ---------------------------------------------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [kql-queries.md](kql-queries.md)                     | Phase 2, 2.1                | KQL query templates for App Insights + SQL metric queries. Includes `--offset` mandate, variable substitution, execution sequence.                                                    |
| [triage-decision-tree.md](triage-decision-tree.md)   | Phase 3                     | Classification logic for request failures, dependency failures, performance/SLA violations, and exceptions. Priority assignment matrix.                                               |
| [sql-assessment.md](sql-assessment.md)               | Phase 3.1                   | SQL health assessment rubric. Correlation analysis template, evidence table structure, spike detection.                                                                               |
| [s360-compliance.md](s360-compliance.md)             | Phase S360-1 through S360-4 | S360 MCP query patterns, SLA-based priority, actionability classification, remediation step generation, S360 briefing format, auto-fix delegation.                                    |
| [remediation-templates.md](remediation-templates.md) | Phase 4, 5, 6, 6.5, 7       | Bug/IcM draft templates, work item dedup logic, code fix delegation prompts, briefing presentation format, report file format, action execution procedures, work item status summary. |
| [team-onboarding.md](team-onboarding.md)             | Phase 1 (first-time)        | Interactive team config YAML generation. Auto-detects App Insights, ADO, SQL, S360. Validates inputs, scaffolds directories, creates wisdom file. Config schema reference.            |

## Loading Convention

The agent orchestrator references skills by phase:

```
Phase 1 (no config) → Load: health-triage/team-onboarding
Phase 2  → Load: health-triage/kql-queries
Phase 3  → Load: health-triage/triage-decision-tree
Phase 3.1 → Load: health-triage/sql-assessment
Phase 4-7 → Load: health-triage/remediation-templates
Phase S360-* → Load: health-triage/s360-compliance
```

Skills are self-contained — each file has all the information needed to execute its phase(s) without referencing other skill files. Cross-phase data (e.g., Query 1 results feeding triage) flows through the agent orchestrator.

## Extensibility

Add new capabilities by creating files in this folder:

- `custom-dashboards.md` — Grafana/Azure Dashboard generation templates
- `capacity-planning.md` — Capacity analysis queries and thresholds
- `postmortem-templates.md` — Incident postmortem generation
- `scripts/` — Shell/PowerShell automation scripts
- `templates/` — YAML/JSON structural templates (currently: `team-config.template.yaml`)
- `scripts/` — Shell/PowerShell automation scripts
- `templates/` — JSON/YAML templates for work items, IcM tickets, etc.
