# UI Telemetry Analysis

This skill file contains KQL query templates for two sub-modes of UI telemetry analysis:

- **Mode 6a — UI Session Replay** (`ui session <SessionId>`): Reconstructs what a user did in a single session using telemetry evidence.
- **Mode 6b — UI Performance Scan** (`ui perf last {N} hours`): Aggregate UI performance analysis — ranks slow UI operations by P75/P95, drills into underlying API dependencies to identify root causes.

**Trigger**:
- Mode 6a: User provides a SessionId, CorrelationId, or ConversationId
- Mode 6b: User asks for UI performance analysis over a time window

**Prerequisite**: Team config must have a `uiTelemetry` section with Log Analytics workspace details. If missing, inform the user: "UI telemetry analysis requires a `uiTelemetry` section in your team config. Add `logAnalytics.workspaceId` and `logAnalytics.resourceId` to enable this mode."

---

## Configuration (from team config)

All values come from the `uiTelemetry` section of the team config YAML:

| Config Key | Description |
|------------|-------------|
| `uiTelemetry.logAnalytics.workspaceId` | Log Analytics workspace GUID for `az monitor log-analytics query --workspace` calls |
| `uiTelemetry.logAnalytics.resourceId` | Full ARM resource path for the Log Analytics workspace |
| `uiTelemetry.appRoleName` | The `AppRoleName` value that identifies this team's UI telemetry in the shared workspace (e.g., `FxP-Client`). Used to scope queries when multiple teams share the same workspace. If omitted, queries run unscoped. |
| `uiTelemetry.reportsDir` | Output directory for session reports (default: `.github/ui-telemetry/reports`) |
| `uiTelemetry.dependencyFilters` | List of `{pattern, description}` pairs for filtering `AppDependencies.Data` |
| `uiTelemetry.telemetrySources` | List of `{sourceId, description}` pairs identifying app-specific telemetry sources (e.g., Redux store loggers, saga trackers). If omitted, platform-level queries still work. |
| `uiTelemetry.noiseActions` | List of high-frequency action names to filter from compact timelines |
| `uiTelemetry.highSignalActions` | List of action names that always indicate important state changes (errors, processing, validation) |
| `uiTelemetry.perfMetricPrefix` | Prefix for UI performance markers in `AppMetrics.Name` (e.g., `PerfCSP`). Used by Mode 6b to query `Name startswith "{prefix}."`. If omitted, Mode 6b scans all `Perf*` metrics. |

---

## Telemetry Primitives

These are the canonical identifiers used across all queries:

- **SessionId**: groups multiple user actions in a session
- **ConversationId**: groups actions within a "conversation" if applicable
- **CorrelationId**: end-to-end trace for a transaction spanning components
- **Timestamp (UTC)**: ordering and duration calculations
- **ChannelId**: identifies entry surface (e.g., "MSXUI", "m365Copilot", "msTeams")
- **ResultCode**: platform enum describing outcome (Success / Failed / CompletedWithIntervention)

---

## Inputs

If the user didn't provide them, ask for **at least one** (use the ask-questions tool):

1. SessionId (preferred)
2. CorrelationId
3. ConversationId

Also ask for:
- Time window bounds (start/end in UTC, or relative like "last 2 days")

---

## CRITICAL: --timespan is MANDATORY on EVERY query

The `az monitor log-analytics query` CLI **defaults to a limited server-side time window**. If you omit `--timespan`, your data may be incomplete.

**MANDATORY**: Every `az monitor log-analytics query` call MUST include `--timespan`:

```
az monitor log-analytics query --workspace {workspaceId} --analytics-query "{query}" --timespan {duration} --output json
```
- "last 24 hours" → `--timespan P1D`
- "last 2 days" → `--timespan P2D`
- "last 7 days" → `--timespan P7D`
- Fixed range → `--timespan "{startUTC}/{endUTC}"`

**NEVER** run `az monitor log-analytics query` without `--timespan`.

---

## Data Collection Sequence

### Step 1: Identify telemetry source
- Query the configured Log Analytics workspace using `az monitor log-analytics query`
- Search across `AppEvents`, `AppRequests`, `AppDependencies`, `AppPageViews`, `AppTraces`, `AppExceptions`, and `AppMetrics` tables tied to the provided session identifier

### Step 2: Pull all events for the session
Sort by Timestamp ascending. Use the query templates below, substituting `{workspaceId}` from config and `{SESSION}` from user input.

### Step 3: Normalize event schema
Map each event to:
- `timestampUtc`
- `eventName`
- `actionCategory` (Navigation / Click / Submit / Load / Validation / Save / API Call / Error)
- `page/screen/component`
- `key dimensions` (scenario, contractId/orderId/operationId if present)
- `resultCode / success flag`
- `latency/duration` (if present)
- `correlation` (operation_Id, CorrelationId, dependency target)

### Step 4: Detect "user intent arcs"
Sequences like: open → navigate → edit → validate → submit → confirmation

### Step 5: Detect issues
Exceptions, failed dependencies, error traces, retries, slow calls (>5s), UX dead-ends

### Step 6: Compute session summary stats
- Session start/end
- Total actions/events
- Key workflow stages reached
- Success vs failure counts by ResultCode

---

## Shared Workspace Architecture

This workspace is shared by multiple teams across the org. All apps log to the same tables (`AppEvents`, `AppDependencies`, etc.) and are distinguished by:

- **`AppRoleName`** — The primary discriminator. Each team's UI produces events with a distinct `AppRoleName` (e.g., `FxP-Client`, `Engage360`). Configured via `uiTelemetry.appRoleName`.
- **`Properties.Source`** — Within a team's events, the `Source` field in `Properties` identifies the telemetry subsystem (e.g., `CPQ.Store.ActionLogger`, `FxpPlatform.AppController`, `GRM.RoleDetails`). Configured via `uiTelemetry.telemetrySources`.

### Common Properties Fields (platform-level, all teams)

These fields are present in `Properties` across all apps on this platform:

| Field | Description |
|-------|-------------|
| `Source` | Telemetry subsystem identifier (e.g., `CPQ.Store.ActionLogger`, `FxpPlatform.Main`) |
| `AppName` | Application name (e.g., `FxpPlatform`) |
| `EnvironmentName` | Deployment environment (`Production`, `Staging`, etc.) |
| `LoggedInUser` | UPN of the signed-in user |
| `ServiceOffering` | Service offering hierarchy |
| `ServiceLine` | Service line hierarchy |
| `Service` | Service name |
| `ComponentId` | Service Tree component GUID |
| `ComponentName` | Service Tree component name |
| `IctoId` | ICTO identifier |
| `BusinessProcessName` | Business process hierarchy |
| `X-Tenant-Id` | Tenant GUID for multi-tenant apps |
| `X-Tenant-Name` | Tenant display name |
| `Path` / `StateName` | Current UI state/route |
| `headerName` / `pageTitle` | Current page context |
| `operation_correlation_id` | Cross-component correlation ID |
| `networkSpeed` | Client network type (e.g., `4g`) |

---

## KQL Variable Substitution

Substitute these variables in all queries:
- `{workspaceId}` → from `uiTelemetry.logAnalytics.workspaceId`
- `{SESSION}` → from user input (SessionId)
- `{APP_ROLE}` → from `uiTelemetry.appRoleName` (if set, adds `| where AppRoleName == "{APP_ROLE}"` line; if not set, omit the line entirely)
- `{SOURCE_FILTER}` → from `uiTelemetry.telemetrySources[0].sourceId` (primary telemetry source; if not configured, use platform-level queries only)
- `{DEP_FILTERS}` → constructed from `uiTelemetry.dependencyFilters` as KQL `or`-joined `contains` clauses (e.g., `Data contains "cpq" or Data contains "clm"`)
- `{NOISE_ACTIONS}` → from `uiTelemetry.noiseActions` as KQL comma-quoted list
- `{HIGH_SIGNAL_ACTIONS}` → from `uiTelemetry.highSignalActions` as KQL comma-quoted list
- `{PERF_PREFIX}` → from `uiTelemetry.perfMetricPrefix` (e.g., `PerfCSP`). If not set, use `Perf` as fallback to match all `Perf*` metrics.

---

## KQL Query Templates

### Tier 1: Platform-Level Queries (work for ALL teams, no app-specific config needed)

These queries use only standard App Insights columns (`SessionId`, `AppRoleName`, `TimeGenerated`) and require only the workspace ID and session ID. They work for any team that logs to this workspace, even without `telemetrySources`, `noiseActions`, or `highSignalActions` configured.

#### Query P1: Session Discovery (find the session, identify the app)

```kusto
union AppEvents, AppPageViews, AppDependencies, AppExceptions
| where SessionId == "{SESSION}"
| summarize
    EventCount = count(),
    Tables = make_set(itemType),
    Apps = make_set(AppRoleName),
    SessionStart = min(TimeGenerated),
    SessionEnd = max(TimeGenerated),
    User = take_any(UserAuthenticatedId)
| extend Duration = SessionEnd - SessionStart
```

Run this FIRST for every session analysis. It tells you which `AppRoleName` the session belongs to, which tables have data, the time range, and the user.

#### Query P2: Full Session Events (all events, all sources)

```kusto
AppEvents
| where SessionId == "{SESSION}"
| sort by TimeGenerated asc
| project TimeGenerated, Name, OperationId,
    Source = tostring(parse_json(Properties).Source),
    User = tostring(parse_json(Properties).LoggedInUser),
    Page = tostring(parse_json(Properties).pageTitle),
    State = tostring(parse_json(Properties).StateName)
| limit 500
```

#### Query P3: Session Page Views & Navigation

```kusto
AppPageViews
| where SessionId == "{SESSION}"
| sort by TimeGenerated asc
| project TimeGenerated, Name, Url, DurationMs, OperationId
```

#### Query P4: Session Exceptions

```kusto
AppExceptions
| where SessionId == "{SESSION}"
| sort by TimeGenerated asc
| project TimeGenerated, ProblemId, ExceptionType=Type, OuterMessage, InnermostMessage, OperationId,
    SeverityLevel
```

#### Query P5: Session Dependencies (all API calls)

```kusto
AppDependencies
| where SessionId == "{SESSION}"
| sort by TimeGenerated asc
| project TimeGenerated, Name, Target, Data, ResultCode, DurationMs, Success, OperationId,
    DependencyType=Type
```

#### Query P6: Session Browser Timings

```kusto
AppBrowserTimings
| where SessionId == "{SESSION}"
| sort by TimeGenerated asc
| project TimeGenerated, Name, Url, TotalDurationMs=TotalDuration, NetworkDurationMs=NetworkDuration,
    SendDurationMs=SendDuration, ReceiveDurationMs=ReceiveDuration, ProcessingDurationMs=ProcessingDuration
```

#### Query P7: Session Client Metrics

```kusto
AppMetrics
| where SessionId == "{SESSION}"
| sort by TimeGenerated asc
| project TimeGenerated, Name, Sum, Min, Max, ItemCount, OperationId
```

#### Query P8: Cross-Table Correlation (trace a single operation)

```kusto
union AppRequests, AppDependencies, AppTraces, AppExceptions, AppEvents
| where operation_Id == "{OP_ID}"
    or tostring(customDimensions["CorrelationId"]) == "{CORR}"
| sort by TimeGenerated asc
| project TimeGenerated, itemType, Name,
    ResultCode = coalesce(tostring(customDimensions["ResultCode"]), ""),
    DurationMs = todouble(customDimensions["DurationMs"]),
    Source = tostring(customDimensions["Source"])
```

#### Query P9: Source Taxonomy Discovery (identify telemetry sources for this session)

```kusto
AppEvents
| where SessionId == "{SESSION}"
| extend Source = tostring(parse_json(Properties).Source)
| summarize Count = count() by Source
| sort by Count desc
```

Use this after P1 to discover the app's telemetry source taxonomy. The top sources tell you what subsystems were active. This is especially useful when `telemetrySources` is NOT configured — the agent can auto-discover the primary store logger.

---

### Tier 2: App-Specific Queries (require `telemetrySources` and filter config)

These queries filter by the team's primary telemetry source and require `telemetrySources`, `noiseActions`, and/or `highSignalActions` to be configured. If not configured, fall back to Tier 1 platform queries.

#### Recommended Tier 2 Query Order
1. Session Summary Stats — scoped to the primary store
2. Action Frequency Breakdown — identify noise vs signal
3. Compact Action Timeline — noise-filtered narrative
4. Non-Primary AppEvents — sagas, platform, SignalR
5. High-Signal Events — errors, processing, state changes
6. Time-Windowed Slice — drill into specific moments
7. Filtered API Dependencies — backend calls matching `dependencyFilters`

#### Query T1: Session Summary Stats (scoped to primary store)

```kusto
AppEvents
| where Properties has "{SOURCE_FILTER}"
| where SessionId == "{SESSION}"
| summarize
    TotalEvents = count(),
    DistinctActions = dcount(Name),
    SessionStart = min(TimeGenerated),
    SessionEnd = max(TimeGenerated)
| extend SessionDuration = SessionEnd - SessionStart
```

#### Query T2: Action Frequency Breakdown

```kusto
AppEvents
| where Properties has "{SOURCE_FILTER}"
| where SessionId == "{SESSION}"
| summarize Count = count() by Name
| sort by Count desc
```

#### Query T3: Compact Action Timeline (noise filtered)

```kusto
AppEvents
| where Properties has "{SOURCE_FILTER}"
| where SessionId == "{SESSION}"
| where Name !in ({NOISE_ACTIONS})
| sort by TimeGenerated asc
| project TimeGenerated, Name
```

#### Query T4: Non-Primary AppEvents

```kusto
AppEvents
| where SessionId == "{SESSION}"
| where Properties !has "{SOURCE_FILTER}"
| sort by TimeGenerated asc
| project TimeGenerated, Name, OperationId, Source=tostring(parse_json(Properties).Source), Properties
```

#### Query T5: High-Signal Events

```kusto
AppEvents
| where Properties has "{SOURCE_FILTER}"
| where SessionId == "{SESSION}"
| where Name in ({HIGH_SIGNAL_ACTIONS})
| sort by TimeGenerated asc
| project TimeGenerated, Name, Properties
```

#### Query T6: Time-Windowed Slice

```kusto
AppEvents
| where Properties has "{SOURCE_FILTER}"
| where SessionId == "{SESSION}"
| where TimeGenerated > datetime("{START_UTC}")
| where TimeGenerated < datetime("{END_UTC}")
| sort by TimeGenerated asc
| project TimeGenerated, Name, Properties
```

#### Query T7: Filtered API Dependencies

```kusto
AppDependencies
| where SessionId == "{SESSION}"
| where {DEP_FILTERS}
| sort by TimeGenerated asc
| project TimeGenerated, Name, DependencyType=Type, Target, Data, ResultCode, DurationMs, OperationId, Properties
```

### Recommended Query Flow

**Mode 6a (Session Replay):**
```
1. Run P1 (Session Discovery) — find the session, identify app, time range
2. Run P9 (Source Taxonomy) — discover telemetry sources for this session
3. If telemetrySources configured:
   → Run T1-T7 (Tier 2 app-specific queries)
4. If NOT configured:
   → Run P2-P7 (Tier 1 platform queries)
   → Use P9 results to identify the primary store logger dynamically
5. Run P8 (Cross-Table Correlation) for specific operations of interest
```

**Mode 6b (Performance Scan):**
```
1. Run UP1 (UI Operations P75/P95) — rank all UI operations by latency
2. For each operation exceeding the P95 threshold:
   a. Run UP2 (Dependency Drill-Down) — find which API calls are slow
   b. Run UP3 (Slow Dependency Details) — get specific slow call examples
3. Run UP4 (Hourly Trend) for top-3 slowest operations
4. Present the UI Performance Briefing
5. Save report
```

---

## Tier 3: UI Performance Queries (Mode 6b)

These queries analyze aggregate UI performance metrics over a time window. They require `uiTelemetry.perfMetricPrefix` for team-scoped analysis (falls back to `Perf` if not configured).

#### Query UP1: UI Operations P75/P95 Ranking

```kusto
AppMetrics
| where TimeGenerated >= datetime("{startUTC}") and TimeGenerated <= datetime("{endUTC}")
| where Name startswith "{PERF_PREFIX}."
| project TimeGenerated, Name, Duration = Sum, ItemCount,
    NetworkSpeed = tostring(parse_json(Properties).networkSpeed),
    ClientCountryOrRegion
| summarize
    P75 = percentile(Duration, 75),
    P95 = percentile(Duration, 95),
    SampleCount = sum(ItemCount)
    by Operation = tostring(split(Name, '.')[1])
| order by P95 desc
```

This is the primary scan query. It ranks all UI operations by P95 latency. Operations with P95 > `thresholds.p95LatencySlaMs` (from team config) are flagged as slow.

#### Query UP2: Dependency Drill-Down for Slow Operation

For each slow UI operation identified by UP1, find the underlying API calls that contribute to the latency:

```kusto
AppDependencies
| where TimeGenerated >= datetime("{startUTC}") and TimeGenerated <= datetime("{endUTC}")
| where OperationName has "{slowOperation}"
| summarize
    P75 = percentile(DurationMs, 75),
    P95 = percentile(DurationMs, 95),
    FailRate = round(100.0 * countif(Success == false) / count(), 2),
    CallCount = count()
    by DependencyName = Name, Target, ResultCode
| order by P95 desc
| take 10
```

#### Query UP3: Slow Dependency Samples

Get specific examples of slow API calls for a given operation:

```kusto
AppDependencies
| where TimeGenerated >= datetime("{startUTC}") and TimeGenerated <= datetime("{endUTC}")
| where OperationName has "{slowOperation}"
| where DurationMs > {p95Threshold}
| project TimeGenerated, Name, Target, Data, ResultCode, DurationMs, Success, OperationId, SessionId
| order by DurationMs desc
| take 10
```

Use this to correlate specific slow calls back to sessions. The `SessionId` can feed into Mode 6a for full session replay.

#### Query UP4: Hourly Trend for Operation

```kusto
AppMetrics
| where TimeGenerated >= datetime("{startUTC}") and TimeGenerated <= datetime("{endUTC}")
| where Name == "{PERF_PREFIX}.{operation}"
| summarize
    P75 = percentile(Sum, 75),
    P95 = percentile(Sum, 95),
    Volume = sum(ItemCount)
    by bin(TimeGenerated, 1h)
| order by TimeGenerated asc
```

#### Query UP5: Geo/Network Breakdown for Operation

```kusto
AppMetrics
| where TimeGenerated >= datetime("{startUTC}") and TimeGenerated <= datetime("{endUTC}")
| where Name == "{PERF_PREFIX}.{operation}"
| extend NetworkSpeed = tostring(parse_json(Properties).networkSpeed)
| summarize
    P95 = percentile(Sum, 95),
    Volume = sum(ItemCount)
    by ClientCountryOrRegion, NetworkSpeed
| order by P95 desc
| take 15
```

Identifies if slowness is geo- or network-specific (e.g., users on 3G in India vs 4G in US).

---

## UI Performance Briefing Format (Mode 6b)

Present the performance scan results in this format:

```
═════════════════════════════════════════════════════════════
 UI PERFORMANCE BRIEFING — {TeamName}
 Period: {startUTC} to {endUTC}
 Operations Scanned: {totalOps} | Slow (P95 > {threshold}ms): {slowCount}
═════════════════════════════════════════════════════════════

── SLOW UI OPERATIONS ({count}) ────────────────────

#{number} {OperationName} — P95: {p95}ms | P75: {p75}ms | Volume: {count}
   Underlying APIs:
   • {apiName} → P95: {apiP95}ms | Fail: {failRate}% | {callCount} calls
   • {apiName} → P95: {apiP95}ms | Fail: {failRate}% | {callCount} calls
   Root Cause: {service processing | partner API | network/geo | mixed}
   Impacted Sessions: `ui session {sampleSessionId}` to investigate

{repeat for each slow operation}

── HEALTHY OPERATIONS ({count}) ────────────────

| Operation | P75 | P95 | Volume |
|-----------|-----|-----|--------|
| {op} | {p75}ms | {p95}ms | {count} |

═════════════════════════════════════════════════════════════
```

The **"Impacted Sessions"** line is the bridge to Mode 6a — it shows a sample SessionId from UP3 so the user can drill into `ui session {id}` for a full replay of a user who experienced the slowness.

---

## Output Format (STRICT)

Produce these sections in this exact order:

### 1) Executive Summary (5-8 sentences)
- What the user attempted
- Whether they succeeded
- Biggest friction points (if any)
- Evidence note: "Based on telemetry events tied to SessionId=... within [time window]."

### 2) Timeline Narrative (chronological)
Write as a readable story with timestamps:
- `[HH:MM:SS] User opened ...`
- `[HH:MM:SS] User navigated to ...`
- `[HH:MM:SS] Validation failed because ...`

Each statement must cite the event(s) that support it: eventName, key dimensions, correlation pointer, result.

### 2b) Session Gantt Chart
Produce a **Mermaid Gantt chart** visualizing the session as a horizontal timeline.

**Construction rules:**
- `gantt` chart type, `dateFormat HH:mm:ss`, `axisFormat %H:%M`
- Title: `Session Timeline — {user} — {contextId}`
- Group tasks into `section` blocks by workflow phase
- Each task: `{label} :{optional_modifier}, {startTime}, {duration}`
  - `:crit,` for errors, failures, high-latency operations (>5s)
  - `:done,` for idle/waiting periods
- Derive start times from `TimeGenerated` of first event in each operation
- Derive durations from span between first/last event, or from `DurationMs`
- Include: platform init, data loading, user actions, high-latency API calls, errors/exceptions, idle gaps
- 15-25 tasks maximum for readability

### 3) Workflow Reconstruction (structured)
Bullets:
- Step 1: ...
- Step 2: ...

For each step: UI surface, key payload/dimensions, outcome.

### 4) Issues & Anomalies
Group by theme:
- **Reliability**: failures / retries / timeouts
- **Performance**: slow loads / dependencies
- **UX**: loops, abandoned steps, repeated clicks

Each issue includes evidence (timestamps + event names + result codes).

### 5) Recommendations
3-7 actionable items:
- Instrumentation fixes (missing fields, better event names, add correlation)
- UX improvements (reduce retries, clearer errors)
- Ops improvements (alerts on ResultCode spikes)

Label speculative suggestions as "Suggested".

### 6) Appendix — Evidence Table (compact)
Top ~15 most relevant events:

| timestampUtc | eventName | screen/component | action | result | correlation |
|---|---|---|---|---|---|

Note "X additional events omitted for brevity" if truncated.

---

## Save Report to File [ALWAYS]

After presenting the report in chat, save a markdown file.

**File naming**: `session-{SessionId}-{YYYY-MM-DD}.md`
**Location**: `{uiTelemetry.reportsDir}` from team config

**YAML frontmatter**:
```yaml
---
session_id: "{SessionId}"
user: "{LoggedInUser}"
context_id: "{contextId}"
session_start: "{firstEventUTC}"
session_end: "{lastEventUTC}"
total_events: {totalEvents}
distinct_actions: {distinctActions}
errors: {errorCount}
generated: "{currentTimestampUTC}"
---
```

Then the full report content (all sections 1-6).

---

## Guardrails
- Do NOT guess user intent beyond what telemetry supports
- Do NOT fabricate counts or durations
- If key fields are missing (SessionId not logged, no correlation), say so and recommend instrumentation
- If multiple sessions match, clearly separate them and state ambiguity
- Assume the report may go to leadership or incident review — keep it professional and evidence-backed
