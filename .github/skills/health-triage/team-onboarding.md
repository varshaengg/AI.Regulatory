# Team Onboarding

This skill file guides the creation of a team configuration YAML file for first-time users of the Health Triage Agent. It runs an interactive discovery flow, validates inputs against live Azure and ADO resources, and generates a complete config with sensible defaults.

**Trigger**: Load this skill when Phase 1 detects no team config YAML file exists in `.github/health-triage/teams/`.

---

## Onboarding Flow

### Step 0: Detect First-Time Usage

This step is handled by the agent orchestrator in Phase 1. If no `.yaml` file exists in `.github/health-triage/teams/`, the agent loads this skill and runs the onboarding flow instead of proceeding with a scan.

Announce to the user:
```
"Welcome! This appears to be your first time using the Health Triage Agent for this service.
I'll walk you through setting up your team configuration — this takes about 2 minutes.
I'll auto-detect what I can and ask you to confirm or fill in the rest."
```

### Step 1: Team Identity

Ask the user (use the ask-questions tool with these questions):

```
Questions:
  1. Team Name — "What is your team/service name?" (e.g., "Customer Success Portfolio")
  2. Team Alias — "Short alias for file naming?" (e.g., "csp") — suggest a lowercase kebab-case version of the team name
```

Use the team alias to name the config file: `.github/health-triage/teams/{teamAlias}.yaml`

### Step 2: Application Insights Discovery

Attempt auto-detection first, then confirm with the user:

```
1. Check current Azure subscription:
   → Run: az account show --query "{name:name, id:id}" -o json
   → Present: "You're logged into subscription: {name} ({id}). Is this correct?"

2. If correct, list App Insights resources:
   → Run: az monitor app-insights component list --query "[].{name:name, id:id, resourceGroup:resourceGroup}" -o table
   → Present the list: "Which Application Insights resource should I monitor?"
   → User selects one

3. If user is in the wrong subscription:
   → Ask: "Which Azure subscription ID should I use?"
   → Run: az account set --subscription {subscriptionId}
   → Then list App Insights (step 2)

4. Extract from selected resource:
   - appInsights.resourcePath: the full resource ID
   - appInsights.subscription: the subscription ID
```

**Validation**: After selection, run a quick test query to verify access:
```shell
az monitor app-insights query --app {resourcePath} --analytics-query "requests | take 1" --offset 1h -o json
```
If this fails, report the error and ask the user to check permissions.

### Step 3: Azure DevOps Configuration

Attempt auto-detection from the workspace, then confirm:

```
1. Check for copilot-instructions.md or .github/copilot-instructions.md:
   → Look for ADO_DEFAULT_PROJECT, ADO_DEFAULT_REPO values
   → If found: "I detected ADO project: {project}, repo: {repo}. Is this correct?"

2. If not found or user says no, ask (use ask-questions tool):
   - ADO Organization — "What is your ADO organization?" (default: "microsoftit")
   - ADO Project — "What is your ADO project?" (e.g., "OneITVSO")
   - Area Path — "What is the area path for Bug work items?" (e.g., "OneITVSO\\Team\\SubTeam")
   - Repository — "Which repository contains your backend/API code?" (e.g., "CE-DM-CSPEx-CSP-ApiCSP")
   - Default Branch — "What is the default branch?" (default: "main")

3. Validate repository exists:
   → Use ado_repo_get_repo_by_name_or_id with the provided repo name
   → If not found: "Repository '{repo}' not found in project '{project}'. Please check the name."
```

### Step 4: SQL Server (Optional)

```
Ask: "Does your service use an Azure SQL Database you'd like to monitor? (yes/no)"

If yes:
  1. Ask: "Provide the full Azure resource ID for the SQL database"
     (e.g., /subscriptions/.../providers/Microsoft.Sql/servers/.../databases/...)

  2. Or help them find it:
     → Run: az sql db list --server {serverName} --resource-group {rg} --query "[].{name:name, id:id}" -o table

  3. Validate:
     → Run: az monitor metrics list --resource {sqlResourcePath} --metrics cpu_percent --interval PT5M --offset 1h -o json
     → If fails: "Could not query SQL metrics. Check the resource ID and permissions."

If no:
  → Set sql.resourcePath to empty/omit from config
```

### Step 5: S360 Configuration (Optional)

```
Ask: "Would you like to enable S360 compliance scanning? (yes/no)"

If yes:
  1. Try auto-detection:
     → Use mcp_service_360_a_search_service_to_repository_mappings
       with serviceNameSearchTerm = {teamName}
     → If found: "I found S360 service: {serviceName} (ID: {serviceId}). Is this correct?"

  2. If not found or user says no:
     → Ask: "What is your S360 service ID?" (GUID)
     → Ask: "What is your S360 service name?"

  3. Ask for additional S360 config:
     → "Team group name?" (e.g., "ESE Delivery Management")
     → "Service group name?" (e.g., "BIC CXP")
     → "Any KPI IDs to actively monitor?" (optional — comma-separated GUIDs)
     → "Team member aliases for filtering?" (comma-separated)
     → "Owned Power Platform environments?" (comma-separated, optional)

If no:
  → Omit entire s360 section from config
```

### Step 6: Telemetry Filters & Thresholds

Use sensible defaults, let user override:

```
Present defaults and ask for confirmation (use ask-questions tool):

1. Reliability Target: 99.95% — "What is your reliability SLO?"
2. P95 Latency SLA: 3000ms — "What is your P95 latency target (ms)?"
3. Failure Count Alert: 50 — "Alert threshold for failures per scan window?"
4. Exception Count Alert: 100 — "Alert threshold for exceptions per scan window?"
5. Exclude Operations: ["get /healthz", "get /default.htm", "get /"]
   → "Any operations to exclude from analysis? (health probes, etc.)"
6. Telemetry Source Filter: "UX"
   → "Exclude telemetry from a specific source? (e.g., 'UX' for frontend-only telemetry)"
```

### Step 7: Dependency Owners (Optional, can be populated later)

```
Ask: "Would you like to map dependency names to owning teams now? (yes/no/later)"

If yes:
  → Ask: "Provide dependency mappings as: dependencyName=TeamName (one per line)"
  → Parse into dependencyOwners map

If no/later:
  → Set dependencyOwners to empty map with a comment:
    # Add dependency → team mappings as you discover them during scans
    # Example: graph.microsoft.com: "Microsoft Graph"
```

---

## Step 8: Generate Config File

**Template file**: `.github/skills/health-triage/templates/team-config.template.yaml`

1. Read the template file from `templates/team-config.template.yaml`
2. Substitute all `{variable}` placeholders with the values collected in Steps 1-7
3. For optional sections (`{sqlSection}`, `{s360Section}`):
   - If configured: expand using the commented examples at the bottom of the template
   - If not configured: remove the placeholder and its surrounding comments entirely
4. Remove the variable substitution key comment block (lines starting with `# Variable substitution key:`)
5. Remove the "Optional sections" reference block at the bottom
6. Write the final YAML to `.github/health-triage/teams/{teamAlias}.yaml`

---

## Step 9: Scaffold Directories & Wisdom File

After writing the config, create the supporting directory structure:

```
1. Create directories (if they don't exist):
   - .github/health-triage/teams/
   - .github/health-triage/reports/
   - .github/health-triage/wisdom/

2. Create initial wisdom file (if it doesn't exist):
   File: .github/health-triage/wisdom/WISDOM.md
   Content:
     # Health Triage Wisdom — {teamName}

     > This file is a living knowledge base maintained by the Health Triage Agent
     > and the Wisdom Miner. It captures patterns, root causes, and investigation
     > insights from past scans to improve future triage accuracy.

     ## Version
     - **Version**: 1.0.0
     - **Created**: {currentDate}
     - **Last Updated**: {currentDate}

     ## Patterns
     <!-- Patterns discovered during health scans will be added here -->

     ## External Dependencies
     <!-- Dependency-specific insights will be added here -->

     ## Known Transients
     <!-- Known transient issues that should not trigger alerts will be added here -->
```

---

## Step 10: Confirm & Offer First Scan

```
Present summary to user:

"Team configuration created successfully!

  📄 Config: .github/health-triage/teams/{teamAlias}.yaml
  📂 Reports: .github/health-triage/reports/
  📚 Wisdom: .github/health-triage/wisdom/WISDOM.md

  Configuration summary:
  - Team: {teamName} ({teamAlias})
  - App Insights: {resourceName}
  - ADO: {adoProject} / {adoRepository}
  - SQL: {configured | not configured}
  - S360: {configured | not configured}
  - Thresholds: {reliabilityTarget}% reliability, {p95LatencySlaMs}ms P95

Would you like to run your first scan now? Say 'scan last 24 hours' to start."
```

---

## Config Schema Reference

This section serves as the definitive schema reference for the team configuration YAML file. Use this when manually editing an existing config or validating generated configs.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `team` | string | Yes | — | Human-readable team/service name |
| `teamAlias` | string | Yes | — | Short alias for file naming (lowercase, kebab-case) |
| `appInsights.resourcePath` | string | Yes | — | Full Azure resource ID for App Insights |
| `appInsights.subscription` | string | Yes | — | Azure subscription ID |
| `thresholds.reliabilityTarget` | number | Yes | 99.95 | Reliability SLO (percent) |
| `thresholds.p95LatencySlaMs` | number | Yes | 3000 | P95 latency target (milliseconds) |
| `thresholds.failureCountAlert` | number | Yes | 50 | Failure count alert threshold per scan |
| `thresholds.exceptionCountAlert` | number | Yes | 100 | Exception count alert threshold per scan |
| `defaultTimeWindow` | string | No | P1D | Default scan window (ISO 8601 duration) |
| `excludeOperations` | list[string] | No | health probes | Operations to exclude from analysis |
| `dependencyOwners` | map[string, string] | No | {} | Dependency name → owning team mapping |
| `ado.organization` | string | Yes | microsoftit | ADO organization |
| `ado.project` | string | Yes | — | ADO project for work items |
| `ado.areaPath` | string | Yes | — | Area path for Bug work items |
| `ado.repository` | string | Yes | — | Repository for code search and PRs |
| `ado.defaultBranch` | string | No | main | Default branch for PRs |
| `sql.resourcePath` | string | No | — | Full Azure resource ID for SQL database |
| `telemetryFilter.excludeSource` | string | No | UX | Telemetry source to exclude |
| `wisdom.wisdomFileLocation` | string | No | .github/health-triage/wisdom/WISDOM.md | Path to wisdom file |
| `workItemDedup.tag` | string | No | HealthTriage | Tag for work item deduplication |
| `workItemDedup.staleThresholdDays` | number | No | 14 | Days before active Bug is flagged stale |
| `s360.serviceId` | string | No | — | S360 service GUID |
| `s360.serviceName` | string | No | — | S360 service display name |
| `s360.teamGroupName` | string | No | — | S360 team group |
| `s360.serviceGroupName` | string | No | — | S360 service group |
| `s360.watchKpiIds` | list[string] | No | [] | KPI IDs to monitor |
| `s360.codeActionableKpiPatterns` | list[string] | No | CodeQL, Security Bugs, ... | Patterns for auto-fix candidates |
| `s360.packageFiles` | list[string] | No | package.json, *.csproj, ... | Files to search for package issues |
| `s360.teamAliases` | list[string] | No | [] | Team member aliases for filtering |
| `s360.ownedEnvironments` | list[string] | No | [] | Power Platform environments owned |
