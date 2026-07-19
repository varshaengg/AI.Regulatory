# SQL Health Assessment

This skill file contains the SQL health assessment rubric, correlation analysis template, and evidence table structures used during Phase 3.1 of the health triage pipeline.

**Prerequisite**: Phase 2.1 SQL metric results and Phase 3 triage classifications must be available before running this assessment.

---

## Role & Objective

You are an **SRE-minded SQL performance analyst and DBA**.
Your task is to produce a **decision-grade SQL health assessment**, not a generic summary, using the findings from the SQL metrics queries in Phase 2.1. Focus on correlating SQL performance metrics with the request performance findings from Application Insights to determine if SQL resource constraints are contributing to service health issues.

## Telemetry Sources

- Application Insights: `{appInsightsPath}` (from team config)
- SQL resource: `{sqlResourcePath}` (from team config `sql.resourcePath`)
- SQL dependency telemetry is enabled (SQL duration, success/failure, correlation identifiers)

---

## Assessment Structure

Produce **one section per finding** that may have SQL involvement (performance findings, dependency failures targeting SQL, timeout exceptions).

### Finding: `{findingId}`

#### A) Summary Assessment

- **Overall SQL health:** `Healthy | Degraded | Unhealthy`
- **Primary symptom(s):**
- **Most likely bottleneck category:**
  `CPU | IO | Memory | Concurrency / Locking | Network | Unknown`
- **Confidence:** `High | Medium | Low`
- **Executive summary:**
  2–4 sentences in plain English explaining what happened and why it matters. Use details from the Phase 2.1 metrics and Phase 3 triage to make this as specific and actionable as possible.

#### B) Evidence Tables (timestamps required)

For **every metric**, include a table with:
- `timestamp` (explicit bin size)
- `value`
- `threshold` (static or dynamic baseline)
- `breach` (`true | false`)
- `notes`

If data is unavailable, still include the table and write `NO DATA` with the reason.

##### 1) SQL Resource Metrics

| timestamp | metric | value | threshold | breach | notes |
|-----------|--------|-------|-----------|--------|-------|

##### 2) App Insights — Request Metrics

| timestamp | operation | metric | value | threshold | breach | notes |
|-----------|-----------|--------|-------|-----------|--------|-------|

##### 3) App Insights — SQL Dependency Metrics

| timestamp | dependency | metric | value | threshold | breach | notes |
|-----------|------------|--------|-------|-----------|--------|-------|

##### 4) Exceptions / Failures (SQL-related)

| timestamp | operation | exception | count | correlated_dependency | notes |
|-----------|-----------|-----------|-------|-----------------------|-------|

#### C) Correlation Analysis

- Correlate **requests → SQL dependencies → exceptions** using shared identifiers
  (e.g., `operation_Id`, parent/child relationships).
- Identify slow or failing requests and show which SQL calls they map to.
- Explicitly describe end-to-end transaction chains where SQL caused user-visible impact.

#### D) Spikes & Abnormalities

For each detected spike or anomaly:
- Spike start/end time
- Magnitude vs baseline
- Metrics affected
- Requests / dependencies that increased concurrently
- Explanation of whether the spike is **causal** or **coincidental**

---

## Bottleneck Category Reference

| Category | Key Metrics | Threshold Guidance |
|----------|------------|-------------------|
| CPU | `cpu_percent` | >80% sustained = degraded, >95% = unhealthy |
| IO | `physical_data_read_percent`, `log_write_percent` | >80% sustained = degraded |
| Memory | `workers_percent`, `sessions_percent` | >80% = degraded |
| Concurrency / Locking | `workers_percent`, `sessions_percent`, deadlock exceptions | >90% workers = unhealthy |
| Storage | `storage_percent`, `allocated_data_storage` | >85% = degraded, >95% = unhealthy |
| TempDb | `tempdb_log_used_percent`, `tempdb_data_size` | >80% = degraded |

---

## Output Integration

The SQL Health Assessment output is included in the Phase 6 Health Briefing under the `── SQL SERVER ASSESSMENT ──` section. Load `health-triage/remediation-templates` skill for the briefing format.
