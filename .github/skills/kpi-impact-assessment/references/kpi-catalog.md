<!-- markdownlint-disable-file -->

# KPI Catalog — Metrics Library

Reference catalog of KPIs organized by category. Select 6–8 metrics for any impact assessment. Baselines must come from the user's provided materials — if unavailable, mark as **N/A**.

## Velocity KPIs

| KPI | Description | Target Direction |
|-----|-------------|-----------------|
| Lead Time for Changes | Time from commit to production deploy | Lower |
| Deployment Frequency | How often code deploys to production | Higher |
| PR Cycle Time | Time from PR open to merge | Lower |
| Sprint Velocity Variance | Standard deviation of story points per sprint | Lower |
| Feature Lead Time | Time from backlog item creation to production release | Lower |
| Time to First Commit | Time from work item assignment to first code commit | Lower |
| Build Duration | CI pipeline execution time (p95) | Lower |
| Code Review Turnaround | Time from review request to first reviewer response | Lower |

## Quality KPIs

| KPI | Description | Target Direction |
|-----|-------------|-----------------|
| Change Failure Rate | % of deployments causing production incidents | Lower |
| Mean Time to Restore (MTTR) | Time to recover from production failure | Lower |
| Escaped Defect Rate | Bugs found in production vs. pre-production | Lower |
| Test Coverage | % of codebase covered by automated tests | Higher |
| Code Review Defect Density | Defects caught per 1,000 lines reviewed | Higher |
| Rework Rate | % of completed work items reopened | Lower |
| Incident Recurrence Rate | % of resolved incidents that recur within 30 days | Lower |
| Security Vulnerability Backlog | Open CodeQL/SAST findings older than 30 days | Lower |

## Cost KPIs

| KPI | Description | Target Direction |
|-----|-------------|-----------------|
| Cost per Deployment | Total cost (infra + labor) per production deploy | Lower |
| Infrastructure Cost per User | Monthly cloud spend divided by active users | Lower |
| Manual Process Labor Cost | Annual cost of manual, automatable tasks | Lower |
| Incident Response Cost | Average cost per production incident (labor + impact) | Lower |
| Developer Toil Ratio | % of engineering time on undifferentiated heavy lifting | Lower |
| License Cost per Developer | Annual tooling/platform cost per engineer | Lower |
| Cloud Waste Ratio | % of provisioned cloud resources idle or oversized | Lower |
| Cost of Delay | Revenue or value lost per sprint of delayed delivery | Lower |

## Risk KPIs

| KPI | Description | Target Direction |
|-----|-------------|-----------------|
| Mean Time to Detect (MTTD) | Time from incident start to detection | Lower |
| Compliance Violation Count | Open S360/regulatory findings | Lower |
| Dependency Vulnerability Age | Average age of unpatched vulnerable dependencies | Lower |
| Recovery Point Objective (RPO) | Maximum acceptable data loss window | Lower |
| Recovery Time Objective (RTO) | Maximum acceptable downtime | Lower |
| Unplanned Work Ratio | % of sprint capacity consumed by unplanned work | Lower |
| Single Point of Failure Count | Systems or processes dependent on one person/component | Lower |

## Experience KPIs

| KPI | Description | Target Direction |
|-----|-------------|-----------------|
| Developer Satisfaction (DevEx) | Survey score on tooling and workflow satisfaction | Higher |
| Onboarding Time to Productivity | Days for a new team member to submit first production PR | Lower |
| Inner Loop Iteration Time | Time from code change to local validation feedback | Lower |
| Context Switching Frequency | Number of tool/system switches per task | Lower |
| Documentation Freshness | % of docs updated within the last quarter | Higher |

## Compliance KPIs

| KPI | Description | Target Direction |
|-----|-------------|-----------------|
| S360 KPI Pass Rate | % of S360 KPIs meeting green status | Higher |
| Privacy Manifest Coverage | % of telemetry events mapped in privacy manifest | Higher |
| SDL Task Completion Rate | % of SDL tasks completed before release | Higher |
| Threat Model Currency | Days since last threat model update | Lower |
| Audit Finding Closure Rate | % of audit findings closed within SLA | Higher |

## Usage Notes

- **Baselines must come from user-provided materials** — if not available, mark as **N/A**
- **Select KPIs that match the initiative** — not every initiative needs all categories
- **Set realistic targets** based on the team's current state and capacity
- **Custom KPIs are allowed** — define them using the same field structure
