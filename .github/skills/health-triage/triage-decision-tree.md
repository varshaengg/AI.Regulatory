# Triage Decision Tree

This skill file contains the classification logic, priority assignment matrix, and wisdom cross-reference patterns used during Phase 3 (Triage and Classification) of the health triage pipeline.

---

## Wisdom Integration

Before classifying any finding, check the wisdom file (location from team config `wisdom.wisdomFileLocation`) for matching patterns. If a finding matches a known pattern:

- Use the wisdom file's classification and root cause as the starting point
- Reference the specific wisdom section that applies (e.g., "This matches 'External Dependencies → {dependency} errors are {team}'s fault'")
- Override the decision tree classification only when wisdom provides stronger evidence
- If the finding is NEW (not in wisdom), flag it for post-scan wisdom update

---

## Request Failures (from KQL Query 1)

```
For each failing operation:
1. Check Query 6 stack traces:
   a. Stack trace points to team-owned code (matches ado.repository from config)?
      → Search codebase: ado_search_code for the file/method
      → Classify: CODE BUG
      → Note source file and line number

   b. Stack trace shows external dependency error (HTTP 502/503/504, timeout)?
      → Check if dependency name matches Query 2 dependency failures
      → Classify: DEPENDENCY-CAUSED (link to dependency finding)
      → No separate action — covered by dependency category

   c. No stack trace but resultCode is 5xx?
      → Check operation's dependency calls in dependencies table
      → If external dependency failed → DEPENDENCY-CAUSED
      → If no dependency failure → CODE BUG (unhandled error)

   d. Very low failure count (<5) and resolved within 30 min?
      → Classify: TRANSIENT
      → No action required

2. Assign priority:
   - P1: Failure > 100 OR reliability impact > 0.5%
   - P2: Failure > 50 OR new failure mode (not seen in prior 7 days)
   - P3: Failure > 10 OR trending upward
   - P4: Failure < 10, transient, known pattern
```

---

## Dependency Failures (from KQL Query 2)

```
For each failing dependency:
1. Look up dependency name in config dependencyOwners:
   a. Found in dependencyOwners?
      → Owner is known. Classify: PARTNER/EXTERNAL FAILURE
      → Action: Draft IcM to the mapped team

   b. Not in dependencyOwners but target is external URL?
      → Classify: PARTNER/EXTERNAL FAILURE (unknown owner)
      → Action: Draft IcM, flag that owner needs to be identified

   c. Dependency is internal (team-owned service)?
      → Check if your service is calling it incorrectly (bad URL, wrong auth, etc.)
      → Classify: INTEGRATION BUG
      → Action: Draft Bug work item

2. Check for correlation:
   - Does this dependency failure explain request failures from Query 1?
   - If yes, link the findings and propose ONE action (not separate tickets)

3. Assign priority:
   - P1: Count > 5000 OR critical dependency (auth, storage, database)
   - P2: Count > 1000 OR new dependency failure
   - P3: Count > 100
   - P4: Count < 100
```

---

## Performance / SLA Violations (from KQL Query 4)

```
For each slow operation (P95 > thresholds.p95LatencySlaMs):
1. Investigate time split:
   - Query dependencies for that operation to find where time is spent
   - Calculate: service processing time vs. external dependency wait time

   a. External dependency accounts for >80% of duration?
      → Classify: PARTNER SLA VIOLATION
      → Action: Draft IcM with P95 evidence

   b. Service code accounts for >50% of duration?
      → Search codebase for the operation handler
      → Look for: N+1 queries, missing caching, synchronous blocking
      → Classify: PERFORMANCE BUG
      → Action: Draft Bug with perf profile

   c. Mixed — both service and dependency contribute?
      → Classify: MIXED PERFORMANCE ISSUE
      → Action: Draft Bug for service optimization + flag partner latency

2. Assign priority:
   - P1: P95 > 5x SLA target
   - P2: P95 > 3x SLA target
   - P3: P95 > 1.5x SLA target
   - P4: P95 > SLA target but < 1.5x
```

---

## Exception Analysis (from KQL Query 3)

```
For each exception type:
1. Is this exception already explained by a request failure or dependency failure finding?
   → Yes: Link to that finding. Do not create a separate action.
   → No: Continue to step 2.

2. Search codebase for the exception type:
   - ado_search_code for the exception class name in the team's repo (ado.repository from config)
   - If found → potential CODE BUG or EDGE CASE

3. Is this a new exception type (not seen in prior 7 days)?
   → Flag as NEW with investigation recommendation

4. Exception types that are always service-fixable:
   - NullReferenceException → CODE BUG (auto-PR candidate)
   - ArgumentNullException → CODE BUG (auto-PR candidate)
   - InvalidOperationException → EDGE CASE (needs investigation)
   - UriFormatException → CODE BUG (input validation gap)
```

---

## Classification Summary

| Classification | Owner | Action Type | Auto-fixable |
|---------------|-------|-------------|--------------|
| CODE BUG | Service team | Bug work item | Yes (via task-researcher → task-planner) |
| INTEGRATION BUG | Service team | Bug work item | Possibly |
| PERFORMANCE BUG | Service team | Bug work item | Possibly |
| DEPENDENCY-CAUSED | Linked to partner | None (covered by dependency) | No |
| PARTNER/EXTERNAL FAILURE | Partner team | IcM ticket | No |
| PARTNER SLA VIOLATION | Partner team | IcM ticket | No |
| MIXED PERFORMANCE ISSUE | Service + Partner | Bug + IcM | Partial |
| TRANSIENT | N/A | Informational | No |
| EDGE CASE | Service team | Investigation | No |

---

## Correlation Rules

1. **One root cause = one action** — If a dependency failure explains multiple request failures, propose one IcM ticket (not one per request failure).
2. **Cascade linking** — When classifying a request failure as DEPENDENCY-CAUSED, explicitly link it to the dependency failure finding by number.
3. **Exception dedup** — If an exception type is already covered by a request or dependency finding, link it rather than creating a separate action.
4. **Regression detection** — If a finding matches a previously-closed Bug work item (from ADO dedup query), flag as REGRESSION and propose a new Bug referencing the old one.
