# KQL Queries & Data Collection

This skill file contains all KQL query templates and SQL metric queries used during Phase 2 (App Insights data collection) and Phase 2.1 (SQL Server metrics collection) of the health triage pipeline.

---

## CRITICAL: --offset / --start-time/--end-time is MANDATORY on EVERY query

The `az monitor app-insights query` CLI **defaults to a ~12-hour server-side time window**. If you omit `--offset` (or `--start-time`/`--end-time`), your data is incomplete and your KQL `where timestamp` filters are useless because the server already truncated the data before KQL runs.

**MANDATORY**: Every `az monitor app-insights query` call MUST include ONE of:

+ Option A (preferred for scan windows): `--offset {duration}` where duration >= scan window
```
az monitor app-insights query --app {appInsightsPath} --analytics-query "{query}" --offset {timeWindowDuration} --output json
```
- "last 24 hours" → `--offset 24h`
- "last 2 days" → `--offset 2d`
- "last 7 days" → `--offset 7d`

+ Option B (for fixed time ranges): `--start-time` and `--end-time` with ISO 8601 timestamps
```
az monitor app-insights query --app {appInsightsPath} --analytics-query "{query}" --start-time {startUTC} --end-time {endUTC} --output json
```

**NEVER** run `az monitor app-insights query` without `--offset` or `--start-time`/`--end-time`. If you do, your data is incomplete and triage will be based on wrong numbers.

---

## Variable Substitution Reference

All queries use these variables, sourced from the team config and the user's scan request:

| Variable | Source | Example |
|----------|--------|---------|
| `{startUTC}` | User's time window start | `2026-03-07T09:30:00Z` |
| `{endUTC}` | User's time window end | `2026-03-08T09:30:00Z` |
| `{excludeSource}` | `telemetryFilter.excludeSource` from config | `UX` |
| `{excludeOpsCSV}` | `excludeOperations` from config, comma-separated, quoted | `'get /healthz','get /default.htm','get /'` |
| `{appInsightsPath}` | `appInsights.resourcePath` from config | `/subscriptions/.../components/app-insights-...` |
| `{sqlResourcePath}` | `sql.resourcePath` from config | Full Azure resource ID for SQL Server |
| `{timeWindowDuration}` | Computed from scan window | `24h`, `2d`, `7d` |
| `{failingOperationName}` | From Query 1 results | `post /api/routepartners` |

---

## App Insights KQL Queries (Phase 2)

### Query 1: Top 10 Request Failures by Operation

```kusto
requests
| where timestamp >= datetime('{startUTC}') and timestamp <= datetime('{endUTC}')
| where customDimensions.TelemetrySource != "{excludeSource}"
| where operation_Name !in ({excludeOpsCSV})
| extend real_operation = replace('/\\d+', '/{0}', tolower(operation_Name))
| summarize Failure=countif(resultCode startswith "5"), Requests=count() by real_operation
| where Failure > 0
| top 10 by Failure desc
```

### Query 2: Top 10 Dependency Failures by Name

```kusto
dependencies
| where timestamp >= datetime('{startUTC}') and timestamp <= datetime('{endUTC}')
| where success == false
| where customDimensions.TelemetrySource != "{excludeSource}"
| summarize Count=sum(itemCount) by name
| top 10 by Count desc
```

### Query 3: Top 10 Exception Types by Count

```kusto
exceptions
| where timestamp >= datetime('{startUTC}') and timestamp <= datetime('{endUTC}')
| where customDimensions.TelemetrySource != "{excludeSource}"
| summarize Count=count() by type, outerMessage
| top 10 by Count desc
```

### Query 4: Top 20 Operations by P95 Duration

```kusto
requests
| where timestamp >= datetime('{startUTC}') and timestamp <= datetime('{endUTC}')
| where customDimensions.TelemetrySource != "{excludeSource}"
| where operation_Name !in ({excludeOpsCSV})
| extend real_operation = replace('/\\d+', '/{0}', tolower(operation_Name))
| summarize P95Duration=percentile(duration, 95), RequestCount=count() by real_operation
| where RequestCount > 10
| top 20 by P95Duration desc
```

### Query 5: Reliability Percentage

```kusto
requests
| where timestamp >= datetime('{startUTC}') and timestamp <= datetime('{endUTC}')
| where customDimensions.TelemetrySource != "{excludeSource}"
| where operation_Name !in ({excludeOpsCSV})
| summarize TotalRequests=count(), FailedRequests=countif(resultCode startswith "5")
| extend Reliability = round((1.0 - (FailedRequests * 1.0 / TotalRequests)) * 100, 5)
```

### Query 6: Exception Stack Traces for Top Failures

Run after Query 1 results are available. For each failing operation, fetch representative stack traces:

```kusto
exceptions
| where timestamp >= datetime('{startUTC}') and timestamp <= datetime('{endUTC}')
| where operation_Name has "{failingOperationName}"
| project timestamp, type, outerMessage, details[0].rawStack, operation_Name
| take 3
```

---

## Execution Sequence (Phase 2)

1. Execute Queries 1-5 in sequence (they are independent but sequential avoids CLI throttling)
2. Execute Query 6 only for operations with Failure > 0 from Query 1 results
3. Parse all JSON results immediately after each query
4. If any query fails, report the error and continue with remaining queries (partial results are acceptable)

---

## SQL Server Metrics Queries (Phase 2.1)

Execute the following monitor queries against Azure Monitor using Azure CLI. Only run these if `sql.resourcePath` is defined in the team config.

### SQL Query 1: CPU Usage

```shell
az monitor metrics list \
  --resource "{sqlResourcePath}" \
  --metrics cpu_percent physical_data_read_percent log_write_percent \
  --aggregation Average \
  --interval PT5M \
  --offset {timeWindowDuration} \
  --output json
```

### SQL Query 2: Storage Pressure

```shell
az monitor metrics list \
  --resource "{sqlResourcePath}" \
  --metrics storage_percent allocated_data_storage \
  --aggregation Maximum \
  --interval PT5M \
  --offset {timeWindowDuration} \
  --output json
```

### SQL Query 3: IO Pressure

```shell
az monitor metrics list \
  --resource "{sqlResourcePath}" \
  --metrics physical_data_read_percent log_write_percent \
  --aggregation Maximum \
  --interval PT5M \
  --offset {timeWindowDuration} \
  --output json
```

### SQL Query 4: Memory and Workers

```shell
az monitor metrics list \
  --resource "{sqlResourcePath}" \
  --metrics workers_percent sessions_percent \
  --aggregation Maximum \
  --interval PT5M \
  --offset {timeWindowDuration} \
  --output json
```

### SQL Query 5: TempDb

```shell
az monitor metrics list \
  --resource "{sqlResourcePath}" \
  --metrics tempdb_log_used_percent tempdb_data_size \
  --aggregation Maximum \
  --interval PT5M \
  --offset {timeWindowDuration} \
  --output json
```

---

## Execution Sequence (Phase 2.1)

1. Check if `sql.resourcePath` is defined in team config — skip all SQL queries if not
2. Execute SQL Queries 1-5 sequentially
3. Results feed into Phase 3.1 (SQL Health Assessment) — load `health-triage/sql-assessment` skill for that phase
