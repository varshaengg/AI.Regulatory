# S360 Compliance

This skill file contains all S360 MCP query patterns, SLA-based priority assignment, actionability classification, remediation step generation, auto-fix delegation prompts, and the S360 compliance briefing format used during Phase S360-1 through S360-4 of the health triage pipeline.

---

## Phase S360-1: S360 Data Collection

### Step 1: Resolve Service Identity

```
1. Read s360.serviceId from team config
2. If not present, search by service name:
   → mcp_service_360_a_search_service_to_repository_mappings
     with serviceNameSearchTerm = {team name from config}
   → Extract serviceId from the response
3. Confirm: "S360 Service: {serviceName} (ID: {serviceId})"
```

### Step 2: Fetch Active Action Items

```
Query by KPI IDs that commonly produce code-actionable items:

1. Search for action items targeting the service:
   → mcp_service_360_a_search_active_s360_kpi_action_items
     with targetIds = ["{serviceId}"]

2. If empty (people-based KPIs), search by known KPI IDs from config:
   → For each kpiId in s360.watchKpiIds (from team config):
     → mcp_service_360_a_search_active_s360_kpi_action_items
       with kpiIds = ["{kpiId}"]
   → Filter results to items where CustomDimensions contain
     service-relevant identifiers (repo names, environment names,
     team aliases matching the team config)

3. For each action item returned, extract:
   - Title, AssignedTo, SLAState, CurrentDueDate
   - CustomDimensions: RuleId, RuleDisplayName, RemediationGuide,
     ResourceType, EnvironmentName, ControlSummary
   - KpiId (to fetch KPI metadata if needed)
```

### Step 3: Fetch KPI Metadata for Context

```
For each unique KpiId found in action items:
  → mcp_service_360_a_get_s360_kpi_metadata_by_kpi_id
    with kpiId = "{kpiId}"
  → Extract: displayName, kpiType, kpiStyle, domainId,
    description (parse remediation steps from HTML),
    tsgLinks, supportChannel
```

### Step 4: Check Resolved Items (Trend Analysis)

```
If user specified a time window:
  → mcp_service_360_a_search_resolved_s360_kpi_action_items
    with targetIds = ["{serviceId}"]
  → Count resolved items in the window
  → Calculate trend: items opening vs. resolving
```

---

## Phase S360-2: S360 Triage and Prioritization

### SLA-Based Priority Assignment

```
For each active action item:

1. Determine SLA urgency:
   a. SLAState == "OutOfSla" (past due):
      → CRITICAL — overdue, immediate attention required
      → Calculate: days overdue = today - CurrentDueDate

   b. SLAState == "NearSla" (approaching due date):
      → HIGH — due within 14 days
      → Calculate: days remaining = CurrentDueDate - today

   c. SLAState == "InSla" (within SLA):
      → MEDIUM — due date > 14 days out
      → Calculate: days remaining = CurrentDueDate - today

   d. SLAState == "AtRisk":
      → HIGH — flagged at risk, treat as near-SLA

2. Assign execution priority (tiebreaker when same SLA state):
   - P1: OutOfSla by >30 days — requires escalation
   - P2: OutOfSla by 1-30 days — urgent remediation
   - P3: NearSla / AtRisk — prioritize this sprint
   - P4: InSla with >30 days remaining — plan for next sprint
```

### Actionability Classification

```
For each action item, classify by fix type:

1. CODE-FIXABLE items (can generate a PR):
   a. CodeQL findings (KPI contains "CodeQL" or "Security Bugs"):
      → Search codebase for the flagged pattern
      → ado_search_code for file/method referenced in the finding
      → If found: CODE-FIX (auto-PR candidate)

   b. Component Governance / Deprecated Packages
      (RuleId matches CG-*, or Title contains "deprecated",
       "vulnerable package", "component governance"):
      → Search package files (package.json, Directory.Packages.props,
        *.csproj) for the flagged package
      → If found: PACKAGE-UPDATE (auto-PR candidate)
      → Identify replacement package from RemediationGuide

   c. Deprecated Connectors (RuleId matches ZN_P*, LCNC-PP-*):
      → These are Power Platform connector issues
      → Search for connector references in code/config
      → If found in code: CODE-FIX
      → If Power Platform only: MANUAL-REMEDIATION

   d. Security configuration (hardcoded secrets, insecure transport):
      → ado_search_code for patterns (connection strings, API keys, http://)
      → If found: CODE-FIX (auto-PR candidate)

2. MANUAL-REMEDIATION items (need human action):
   a. Service Tree metadata updates
   b. Azure AD / Entra configuration changes
   c. Power Platform environment-level settings
   d. Personnel/access policy actions
   e. IcM connector routing setup

   → Provide step-by-step remediation guide from KPI metadata
   → Link to RemediationGuide URL
   → Optionally draft ADO work item for tracking

3. DELEGATION items (need another team):
   a. Action item AssignedTo is not a team member
   b. EnvironmentName doesn't match team's environments
   → Flag: "Assigned to {alias} — verify this is a team member"
   → If not team member: recommend reassignment or exception request
```

### Remediation Steps Generation

```
For each action item, produce a concrete step list:

1. Parse the KPI description (HTML → plaintext) for official guidance
2. Extract RemediationGuide URL from CustomDimensions
3. Extract TSG links from KPI metadata
4. Generate ordered steps:

   For CODE-FIXABLE:
     Step 1: Locate the issue — {file path, line, pattern}
     Step 2: Apply fix — {specific code change description}
     Step 3: Run tests — {test command from repo config}
     Step 4: Create PR — targeting {defaultBranch}
     Step 5: After merge, allow up to {dataLatency} hours for S360 scan refresh

   For PACKAGE-UPDATE:
     Step 1: Identify current version — {package}@{currentVersion}
     Step 2: Update to — {package}@{safeVersion} (from remediation guide)
     Step 3: Run build and tests
     Step 4: Check for breaking changes in changelog
     Step 5: Create PR

   For MANUAL-REMEDIATION:
     Step 1: {first action from KPI description}
     Step 2: {second action}
     ...
     Step N: Verify in S360 portal (allow {dataLatency} hours for refresh)
     Reference: {RemediationGuide URL}
     Support: {supportChannel} — {support contact from KPI metadata}
```

---

## Phase S360-3: S360 Code Fix via Auto-PR Pipeline

**Trigger**: Any S360 action item classified as CODE-FIX or PACKAGE-UPDATE.

Uses the same @task-researcher → @task-planner pipeline as App Insights code fixes, with S360-specific context.

### For CODE-FIX items (CodeQL, security patterns):

```
Prepare context package:

Context Package:
  - S360 KPI: {kpiDisplayName}
  - Action Item Title: {title}
  - Rule: {RuleId} — {RuleDisplayName}
  - Control: {ControlSummary}
  - SLA State: {SLAState} (due: {CurrentDueDate})
  - Remediation Guide: {RemediationGuide URL}
  - Repository: {from team config — ado.repository}
  - Branch: {from team config — ado.defaultBranch}
  - Code search results: {file paths and matches from ado_search_code}

Delegate to @task-researcher with prompt:

"Research an S360 compliance finding detected by the Health Triage Agent.

## S360 Finding
- KPI: {kpiDisplayName}
- Rule: {RuleId} — {RuleDisplayName}
- Control: {ControlSummary}
- SLA: {SLAState} — due {CurrentDueDate}
- Remediation guide: {RemediationGuide URL}

## Code Location
{code search results — file paths, line numbers, matching patterns}

## Research Objectives
1. Locate all instances of the flagged pattern in the repository
2. Understand why the pattern is flagged (security risk from the remediation guide)
3. Determine the compliant replacement pattern
4. Identify all affected files (not just the first match)
5. Check for existing tests covering the affected code
6. Verify the fix doesn't break existing functionality

## ADO Configuration
Use project: {ado.project}
Area path: {ado.areaPath}

Please create a research document and then hand off to @task-planner for implementation planning."
```

### For PACKAGE-UPDATE items (Component Governance, deprecated packages):

```
Prepare context package:

Context Package:
  - S360 KPI: {kpiDisplayName}
  - Action Item Title: {title}
  - Flagged Package: {packageName}@{currentVersion}
  - Replacement: {suggestedReplacement} (from remediation guide)
  - SLA State: {SLAState} (due: {CurrentDueDate})
  - Package File: {package.json | Directory.Packages.props | *.csproj}
  - Repository: {from team config — ado.repository}
  - Branch: {from team config — ado.defaultBranch}

Delegate to @task-researcher with prompt:

"Research an S360 package compliance finding detected by the Health Triage Agent.

## S360 Finding
- KPI: {kpiDisplayName}
- Flagged Package: {packageName}@{currentVersion}
- Issue: {title} (e.g., deprecated, vulnerable, license non-compliant)
- SLA: {SLAState} — due {CurrentDueDate}
- Suggested replacement: {replacement from remediation guide}

## Research Objectives
1. Find all references to {packageName} in the repository (package files, imports, usages)
2. Identify the safe/compliant version or replacement package
3. Check the package changelog for breaking changes between {currentVersion} and target
4. Identify all consuming code that may need updates
5. Determine if a simple version bump suffices or if API migration is needed
6. Check for transitive dependencies that also pull in the flagged package

## ADO Configuration
Use project: {ado.project}
Area path: {ado.areaPath}

Please create a research document and then hand off to @task-planner for implementation planning."
```

### Safety rules for S360 auto-fixes:
- Maximum 3 S360 code fix delegations per scan
- Engineer must approve each delegation before it starts
- Package major version bumps require explicit engineer confirmation
- All changes go through PR review — nothing is auto-merged
- After PR merge, note in briefing: "Allow up to {dataLatency} hours for S360 to reflect the fix"

---

## Phase S360-4: S360 Compliance Briefing Format

Present S360 findings in this structured format:

```
═══════════════════════════════════════════════════════════════
S360 COMPLIANCE BRIEFING — {TeamName}
Service: {serviceName} (ID: {serviceId})
Scan Date: {currentDate}
Active Action Items: {totalActive} | Overdue: {overdueCount} | Near SLA: {nearSlaCount}
═══════════════════════════════════════════════════════════════

── CRITICAL — OVERDUE ({count}) ────────────────────────────

#{number} [P{priority}] {KPI Name} — {Action Item Title}
   Assigned To: {alias}
   Due Date: {CurrentDueDate} ({daysOverdue} days overdue)
   Rule: {RuleId} — {ControlSummary}
   Fix Type: {CODE-FIX | PACKAGE-UPDATE | MANUAL-REMEDIATION}
   Remediation Steps:
     1. {step 1}
     2. {step 2}
     ...
   Reference: {RemediationGuide URL}
   ┌───────────────────────────────────────────────────────────┐
   │  [Create Bug + Auto-Fix PR]  [Create Bug Only]  [Skip]   │
   └───────────────────────────────────────────────────────────┘

── HIGH — NEAR SLA ({count}) ───────────────────────────────

#{number} [P{priority}] {KPI Name} — {Action Item Title}
   Assigned To: {alias}
   Due Date: {CurrentDueDate} ({daysRemaining} days remaining)
   Rule: {RuleId} — {ControlSummary}
   Fix Type: {CODE-FIX | PACKAGE-UPDATE | MANUAL-REMEDIATION}
   Remediation Steps:
     1. {step 1}
     2. {step 2}
     ...
   Reference: {RemediationGuide URL}
   ┌───────────────────────────────────────────────────────────┐
   │  [Create Bug + Auto-Fix PR]  [Create Bug Only]  [Skip]   │
   └───────────────────────────────────────────────────────────┘

── MEDIUM — IN SLA ({count}) ───────────────────────────────

#{number} [P{priority}] {KPI Name} — {Action Item Title}
   Assigned To: {alias}
   Due Date: {CurrentDueDate} ({daysRemaining} days remaining)
   Rule: {RuleId} — {ControlSummary}
   Fix Type: {CODE-FIX | PACKAGE-UPDATE | MANUAL-REMEDIATION}
   Steps: {abbreviated — first 2 steps}
   Reference: {RemediationGuide URL}
   ┌──────────────────────────────────────────┐
   │  [Create Bug]  [Plan for Sprint]  [Skip] │
   └──────────────────────────────────────────┘

── TREND ───────────────────────────────────────────────────

Items resolved recently: {resolvedCount}
Items opened recently: {newCount}
Trend: {IMPROVING ↑ | STABLE → | DEGRADING ↓}

── AUTO-FIX PIPELINE (S360) ({count}) ──────────────────────

{Show only if S360 code fix delegations were approved}
S360 #{number}: {Rule} — {Title}
   Pipeline: @task-researcher → @task-planner → Copilot
   Status: {Pending approval | Delegated | Research complete | Plan created | PR ready}
   Research: .copilot-tracking/research/{date}-s360-{ruleId}-research.md
   [Delegate Now]  [Create Bug Only]  [Skip]

═══════════════════════════════════════════════════════════════
S360 SUMMARY: {totalActions} items requiring attention
┌─────────────────────────────────────────────────────────────┐
│ {overdueCount} Overdue | {nearSlaCount} Near SLA            │
│ {codeFixCount} Code-fixable | {manualCount} Manual          │
│ {autoFixCount} Auto-fix candidates                          │
│                                                              │
│ [Fix All Code Items]  [Create Bugs for All]  [Review Each]  │
└─────────────────────────────────────────────────────────────┘
```

### Combined Mode (Mode 5) Format

When running Mode 5 (Full Health + S360), merge both briefings:

```
═══════════════════════════════════════════════════════════════
UNIFIED HEALTH + COMPLIANCE BRIEFING — {TeamName}
Period: {startUTC} to {endUTC}
═══════════════════════════════════════════════════════════════

Part 1: Service Health (Application Insights)
{...Health briefing content from remediation-templates.md...}

Part 2: S360 Compliance
{...S360 briefing content above...}

═══════════════════════════════════════════════════════════════
COMBINED SUMMARY
Health Score: {score}/100 | S360 Items: {totalS360}
{healthActions} health actions + {s360Actions} S360 actions = {total} total
└─ [Approve All]  [Review Health]  [Review S360]  [Export]
═══════════════════════════════════════════════════════════════
```
