# Remediation Templates

This skill file contains Bug/IcM draft templates, work item deduplication logic, code fix delegation prompts, the health briefing presentation format, report file format, action execution procedures, and work item status summary — covering Phase 4, 5, 6, 6.5, 7, and the Work Item Status Summary phase.

---

## ADO Work Item Deduplication (Phase 5/7 prerequisite)

**Principle**: ADO is the single source of truth for work item tracking. Do NOT maintain local tracking files. Query ADO directly to check for duplicates before proposing new Bug work items.

### Dedup Check Procedure

Before proposing any Bug work item, query ADO for existing active items:

```
1. Use ado_search_workitem or ado_wit_get_query to search for:
   - Work item type: Bug
   - Tags contains: "HealthTriage"
   - State NOT IN ("Closed", "Removed", "Resolved")
   - Title contains keywords from the finding (operation name, exception type)
   - Project: {ado.project} from team config

2. Evaluate results:
   a. MATCH FOUND (active Bug exists for this finding):
      → Reference the existing Bug ID in your triage notes
      → Add a comment to the existing Bug with new error counts and scan timestamp
        using ado_wit_add_work_item_comment
      → Do NOT propose a new Bug
      → Report: "Existing Bug #{id} covers this finding — updated with new evidence"

   b. MATCH FOUND but RESOLVED/CLOSED:
      → This is a REGRESSION — the issue has resurfaced after fix
      → Propose a NEW Bug referencing the old one: "Regression of Bug #{oldId}"
      → Flag to user: "This issue was previously fixed (Bug #{oldId}) but has reappeared"

   c. NO MATCH:
      → Propose new Bug work item (see format below)
```

**IMPORTANT**: All Bug work items created by this agent MUST include the tag `HealthTriage`. This is the mechanism for deduplication across scans. Without this tag, the dedup query will not find the item and a duplicate will be created.

---

## Phase 4: Code Fix Delegation (Task-Researcher → Task-Planner Pipeline)

**Trigger**: Any finding classified as CODE BUG (NullReferenceException, ArgumentNullException, UriFormatException, or other service code issues).

### Delegation Workflow

```
Health Triage Agent (orchestrator)
  │
  ├─ Step 1: Prepare context package for the code fix
  │
  ├─ Step 2: Delegate to @task-researcher
  │   └─ Researches the codebase, validates the bug, documents findings
  │   └─ Output: .copilot-tracking/research/{date}-health-triage-{finding}-research.md
  │
  ├─ Step 3: Delegate to @task-planner
  │   └─ Creates implementation plan, checklist, and prompt from research
  │   └─ Output: .copilot-tracking/plans/{date}-health-triage-{finding}-plan.md
  │   └─ Output: .copilot-tracking/details/{date}-health-triage-{finding}-details.md
  │   └─ Output: .copilot-tracking/prompts/{date}-health-triage-{finding}-prompt.md
  │
  └─ Step 4: Task-planner hands off to Copilot for implementation
      └─ Copilot follows the plan to make code changes and create PR
```

### Step 1: Context Package Format

```
For each CODE BUG finding, assemble:

Context Package:
  - Finding ID: #{number} from the health briefing
  - Exception type: {type} (e.g., NullReferenceException)
  - Exception message: {message}
  - Operation: {operation_Name}
  - Failure count: {count} in {timeWindow}
  - Stack trace: {full stack trace from Query 6}
  - Source file (if identified): {filePath}:{lineNumber}
  - KQL evidence query: {the query that detected this}
  - Reliability impact: {percentage impact}
  - Repository: {from team config — ado.repository}
  - Branch: {from team config — ado.defaultBranch}
```

### Step 2: Task-Researcher Delegation Prompt

```
"Research a production bug detected by the Health Triage Agent.

## Bug Context
- Exception: {type} — {message}
- Operation: {operation_Name}
- Failure count: {count} failures in the last {timeWindow}
- Stack trace:
  {stack trace}
- Source file: {filePath}:{lineNumber}
- Repository: {ado.repository}
- Branch: {ado.defaultBranch}

## Research Objectives
1. Locate the failing code in the repository and understand the call chain
2. Identify the root cause (null reference, missing validation, unhandled edge case, etc.)
3. Determine the minimal fix — null check, guard clause, input validation, retry logic, etc.
4. Identify related code paths that may have the same vulnerability
5. Check for existing tests covering this code path
6. Document any patterns in the codebase for handling similar scenarios

## KQL Evidence
{the KQL query that found this issue}

## ADO Configuration
Use project: {ado.project}
Area path: {ado.areaPath}

Please create a research document and then hand off to @task-planner for implementation planning."
```

### Step 3/4: Handoff Chain

- @task-researcher → @task-planner via configured handoff ("Create Implementation Plan from Research")
- @task-planner → Copilot via configured handoff ("Execute Implementation")

### Tracking Output

```
After delegation, record in the briefing:

Finding #{number}: {ExceptionType} in {OperationName}
  Status: Delegated to @task-researcher → @task-planner pipeline
  Research: .copilot-tracking/research/{date}-health-triage-{finding}-research.md
  Plan: .copilot-tracking/plans/{date}-health-triage-{finding}-plan.md

  The task-planner will create a PR after implementation.
  Track progress in .copilot-tracking/ directory.
```

### Code Fix Safety Rules

- Maximum 3 code fix delegations per scan (avoid overwhelming the pipeline)
- Engineer must approve the delegation before it starts
- The task-researcher and task-planner follow their own safety rules (ADO tracking, research validation)
- All code changes go through PR review — nothing is auto-merged
- If the engineer prefers to fix manually, skip delegation and create only the Bug work item

---

## Phase 5: Remediation Proposal Templates

### For CODE BUG findings:

```
Draft ADO Bug work item:
- Type: "Bug"
- Project: {ado.project} (from team config)
- Area Path: {ado.areaPath} (from team config)
- Title: "[Health Triage] {ExceptionType} in {OperationName}"
- Repro Steps:
  ## Evidence
  - Exception: {type} — {message}
  - Failure count: {count} in {timeWindow}
  - Impact: {reliability impact}

  ## KQL Repro Query
  {the query that found this issue}

  ## Stack Trace
  {abbreviated stack trace with service-owned frames highlighted}

  ## Source Location
  File: {filePath}
  Method: {methodName}
  Line: {lineNumber}

  ## Proposed Fix
  {description of the fix or "Delegated to @task-researcher → @task-planner pipeline for automated fix"}
- Priority: {P1-P4 from triage}
- Tags: "HealthTriage; AutoDetected"

Additionally, offer the engineer two options:
  a. [Create Bug Only] — creates the Bug work item for manual fix
  b. [Create Bug + Auto-Fix] — creates the Bug AND delegates to
     @task-researcher → @task-planner pipeline (Phase 4) for automated
     research, planning, and code fix via PR
```

### For CODE BUG findings with existing active Bug:

```
Update existing ADO Bug work item:
- Update the repro steps with a time stamp and new evidence from this scan (additional error counts, related reports)
- Update the work item repro steps with any new findings
- Ensure the work item is still marked as "Active" in the local tracking database
- Keep your notes concise but informative, referencing the new evidence and how it relates to the existing work item

Additionally, update the work item tracking with the new findings from this report as well as update the most recent detection time stamp for this issue in the tracking database to reflect that it is still active and relevant based on the latest scan results.
```

### For PARTNER/EXTERNAL FAILURE findings:

```
Draft IcM ticket (delegate to @support-agent):
- Owning Team: {from dependencyOwners config}
- Title: "[Health Triage] {DependencyName} failures impacting {teamName}"
- Description:
  ## Issue Summary
  {teamName} detected {count} failures from {dependencyName} between {start} and {end} UTC.

  ## Evidence
  - Dependency: {name}
  - Target: {target URL/endpoint}
  - Failure count: {count}
  - Error pattern: {HTTP status codes, error messages}
  - {teamName} impact: {affected operations, reliability impact}

  ## KQL Evidence Query
  {the query that found this issue}

  ## Request
  Please investigate the availability issues on your service during this time window.
```

### For SLA VIOLATION findings:

```
Draft IcM ticket (delegate to @support-agent):
- Owning Team: {from dependencyOwners config}
- Title: "[Health Triage] {PartnerService} P95 latency exceeding SLA"
- Description:
  ## Issue Summary
  {teamName} observed P95 latency of {P95}ms from {partnerService}, exceeding the agreed SLA of {slaTarget}ms.

  ## Evidence
  - Operation: {operationName}
  - P95 Latency: {P95}ms (SLA: {slaTarget}ms)
  - Time split: service processing {serviceTime}ms | Partner response {partnerTime}ms
  - Request volume: {count} requests in {timeWindow}

  ## KQL Evidence Query
  {the query that found this issue}
```

---

## Phase 6: Health Briefing Presentation Format

Present all findings in this category-first briefing format:

```
═══════════════════════════════════════════════════════════════
HEALTH BRIEFING — {TeamName}
Period: {startUTC} to {endUTC}
Health Score: {score}/100 ({status} — {trend} from yesterday)
Reliability: {reliability}% | Requests: {total} | Failed: {failed} | Exceptions: {exceptions}
═══════════════════════════════════════════════════════════════

── REQUEST FAILURES ({count} issues) ───────────────────────

#{number} [{priority}] {operation_name} — {failureCount} failures
   Root Cause: {one-line root cause from triage}
   Investigation: {what the agent checked — stack trace, code search, dependency correlation}
   Category: {classification}
   Proposed: [{action type}] {action description}
   ┌─────────────────────────────────────────┐
   │  [Approve]  [Edit & Approve]  [Reject]  │
   └─────────────────────────────────────────┘

{repeat for each request failure finding}

── DEPENDENCY FAILURES ({count} root causes) ───────────────

#{number} [{priority}] {dependency_name} — {failureCount} failures
   Root Cause: {root cause}
   Investigation: {owner lookup, internal vs external, correlation with request failures}
   Category: {classification}
   Owner: {team from dependencyOwners}
   Proposed: [{action type}] {action description}
   ┌───────────────────────────────────────────────────┐
   │  [Approve IcM]  [Edit Draft]  [Reject]  [Suppress]│
   └───────────────────────────────────────────────────┘

{repeat for each dependency failure finding}

── PERFORMANCE / SLA ({count} issues) ──────────────────────

#{number} [{priority}] {operation_name} — P95: {duration}ms (SLA: {target}ms)
   Root Cause: {time split analysis — service vs partner}
   Investigation: {dependency drill-down, code search for service-side issues}
   Category: {classification}
   Proposed: [{action type}] {action description}
   ┌───────────────────────────────────────────────────┐
   │  [Approve]  [Edit & Approve]  [Reject]  [Suppress]│
   └───────────────────────────────────────────────────┘

{repeat for each performance finding}

── EXCEPTIONS (supporting evidence) ────────────────────────

Top exceptions mapped to findings:
• {ExceptionType} ({count}) → linked to #{findingNumber} ({reason})
• {ExceptionType} ({count}) → NEW — standalone investigation
  └─ [{action type}] {action description}
     [Approve]  [Reject]

── AUTO-FIX PIPELINE ({count}) ─────────────────────────────

{Show only if code fix delegations were approved}
Finding #{number}: {ExceptionType} in {OperationName}
   Pipeline: @task-researcher → @task-planner → Copilot
   Status: {Pending approval | Delegated | Research complete | Plan created | PR ready}
   Research: .copilot-tracking/research/{date}-health-triage-{finding}-research.md
   Plan: .copilot-tracking/plans/{date}-health-triage-{finding}-plan.md
   [Delegate Now]  [Create Bug Only]  [Skip]

═══════════════════════════════════════════════════════════════
SUMMARY: {totalActions} actions pending approval
┌─────────────────────────────────────────────────────────────┐
│ {icmCount} IcM tickets | {bugCount} Bug work items          │
│ {fixCount} Auto-fix pipelines | {infoCount} Informational   │
│                                                              │
│ [Approve All]  [Approve Selected]  [Review Details]          │
└─────────────────────────────────────────────────────────────┘

── SQL SERVER ASSESSMENT ────────────────────────────────────

{Include the full Phase 3.1 SQL Health Assessment output here}

```

---

## Phase 6.5: Save Briefing to File

After presenting the briefing in chat, **always** write the complete briefing to a markdown file for audit, sharing, and historical reference.

**File naming**: `health-triage-{team}-{YYYY-MM-DD}-{HHMMSS}.md`
**Location**: `.github/health-triage/reports/`

```
Action: Save briefing to markdown file

1. Construct file path:
   - Directory: .github/health-triage/reports/
   - Filename: health-triage-{teamAlias}-{scanEndDate}-{scanEndTime}.md
   - Example: health-triage-{teamAlias}-2026-03-08-093000.md
   - Use the scan end timestamp (UTC) for uniqueness

2. Construct file content:
   - Start with YAML frontmatter for metadata:
     ---
     team: {TeamName}
     alias: {teamAlias}
     scan_start: {startUTC}
     scan_end: {endUTC}
     generated: {currentTimestampUTC}
     reliability: {reliability}%
     total_requests: {total}
     failed_requests: {failed}
     total_exceptions: {exceptions}
     health_score: {score}/100
     findings_count: {totalFindings}
     actions_proposed: {totalActions}
     ---

   - Then the full briefing content (same as Phase 6 output)

   - Append an ACTIONS LOG section at the bottom:
     ## Actions Log
     | # | Finding | Category | Action Type | Status |
     |---|---------|----------|-------------|--------|
     | 1 | {finding} | {category} | {IcM/Bug/PR/Info} | Pending |
     ...

     (This section gets updated in Phase 7 when actions are approved/rejected)

3. Create the directory if it doesn't exist

4. Write the file using the create_file tool or terminal

5. Confirm to user:
   "Briefing saved to: .github/health-triage/reports/{filename}"
```

**After Phase 7 (action execution)**, update the saved file's Actions Log with final statuses:

```
| # | Finding | Category | Action Type | Status |
|---|---------|----------|-------------|--------|
| 1 | Azure Blob failures | Partner Failure | IcM | Approved — IcM #12345 |
| 2 | NullRef in ProcessorService | Code Bug | Bug + Auto-Fix | Approved — Bug #67890, fix delegated to @task-researcher → @task-planner |
| 3 | Graph 429 transient | Transient | Informational | No action |
| 4 | N+1 query pattern | Perf Bug | Bug | Rejected — deferred to next sprint |
```

---

## Phase 7: Execute Approved Actions

```
For each approved action:

1. IcM Tickets:
   → Delegate to @support-agent: "Create IcM ticket with this content: {draft}"
   → Confirm: "IcM {icmId} created for {dependencyName} → {owningTeam}"
   → ICM Tags: "HealthTriage; AutoDetected"

2. Bug Work Items (Bug Only):
   → Run dedup check: query ADO for active Bugs tagged "HealthTriage" matching the finding
   → If duplicate found: add comment to existing Bug with new evidence, SKIP creation
   → If no duplicate: call ado_wit_create_work_item with drafted fields + tag "HealthTriage"
   → Confirm: "Bug #{workItemId} created: {title}"

3. Bug Work Items + Auto-Fix (Bug + Fix Pipeline):
   → Run dedup check (same as step 2)
   → If no duplicate: call ado_wit_create_work_item with drafted fields + tag "HealthTriage"
     Include in description: "Fix delegated to task-researcher → task-planner pipeline"
   → Then delegate to @task-researcher with the context package (see Phase 4)
   → The pipeline flows: @task-researcher → @task-planner → Copilot → PR
   → Confirm: "Bug #{workItemId} created. Fix delegated to @task-researcher → @task-planner pipeline.
     Track progress in .copilot-tracking/ directory."

4. Suppressions:
   → Record suppression in session (not persisted for v1)
   → Confirm: "Suppressed {finding} for this session"

Report:
"All approved actions executed:
 - IcM {id1} created → {team1}
 - Bug #{id2} created → {title2}
 - Bug #{id3} created + fix delegated to @task-researcher → @task-planner
 - {finding4} suppressed"
```

---

## Work Item Status Summary (After Triage, Before Wisdom)

After triage, include a summary of all HealthTriage-tagged work items in the report. This is a READ-ONLY status snapshot — no local file to maintain.

```
1. Query ADO for all Bug work items tagged "HealthTriage" in {ado.project}:
   - Use ado_search_workitem: "tag:HealthTriage project:{ado.project}"
   - Or use ado_wit_get_query with appropriate WIQL

2. Group results by state:
   - Active: Items in New/Active/Committed state
   - Recently Resolved: Items closed in the last 7 days
   - Stale: Items in Active state but not updated in >14 days

3. For Active items, check if any match findings from this scan:
   - If matched → note "Still occurring — {new count} failures this scan"
   - If NOT matched → note "Not detected this scan — may be resolved"

4. Include in the health briefing report under:
   ## Tracked Work Items (HealthTriage)
   | ADO ID | Title | State | Last Updated | This Scan |
   |--------|-------|-------|-------------|----------|
   | #12345 | [HT] securityrec P95 | Active | 2026-03-28 | Still occurring (7,104ms) |
   | #12346 | [HT] Stale accounts | Active | 2026-03-25 | Not detected |
   | #12347 | [HT] PPTX table | Closed | 2026-03-27 | N/A |

5. Flag anomalies to the engineer:
   - "Bug #{id} is Active but issue not detected this scan — consider closing"
   - "Bug #{id} was Closed but issue reappeared — regression candidate"
```

**NOTE**: This replaces the file-based Bug Audit phase. ADO is the source of truth — no local sync needed.
