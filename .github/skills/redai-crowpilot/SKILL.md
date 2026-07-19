---
name: redai-crowpilot
description: "AI-powered vulnerability discovery tool for red team operators using RedAICrowpilot. Runs automated security scans in agent or interactive mode against local repos, ADO repos, or Service Tree IDs. USE FOR: vulnerability discovery, red team scanning, security scan, crowpilot scan, run crowpilot, agent mode scan, vulnerability assessment, security testing, code vulnerability scan, red team assessment, scan repo for vulnerabilities, scan ADO repo, scan by Service Tree ID, list crowpilot experts, resume crowpilot session. DO NOT USE FOR: compliance enforcement (use compliance-enforcement skill), SDL compliance (use compliance-enforcement skill), architecture design (use architecture-design skill), deployment (use microsoft-engineering skill)."
license: MIT
metadata:
  author: GHC AI Accelerators
  version: "1.0.0"
---

# RedAI CrowPilot — Vulnerability Discovery Skill

This skill automates vulnerability discovery using **RedAICrowpilot**, an AI-powered security scanning tool for red team operators. It supports interactive mode, agent mode (local path, ADO repo URL, or Service Tree ID), expert selection, and session management.

## When to Activate

- "run crowpilot"
- "scan for vulnerabilities"
- "red team scan"
- "vulnerability discovery"
- "security scan this repo"
- "run crowpilot agent mode"
- "scan ADO repo for vulnerabilities"
- "scan by Service Tree ID"
- "list crowpilot experts"
- "resume crowpilot session"
- "crowpilot scan changed files"
- User asks to discover security vulnerabilities in code

## When NOT to Use

- Compliance enforcement or S360 validation (use `compliance-enforcement` skill)
- Architecture design (use `architecture-design` skill)
- Deployment or infrastructure (use `microsoft-engineering` skill)
- General code review without security focus (use standard review workflows)

## Definitions

- **RedAICrowpilot** → AI-powered vulnerability discovery CLI tool for red team operators, backed by MRT's Foundry
- **Agent Mode** → Automated non-interactive scanning mode (via `--agentPath`, `--agentSTID`, or `--agentRepoUrl`)
- **Interactive Mode** → Default mode when no agent options are provided; guides the user through scan configuration
- **Expert Agents** → Specialized vulnerability discovery modules that can be selected for targeted scanning
- **Session** → A saved scan state that can be resumed or continued later
- **Service Tree ID (STID)** → Microsoft internal identifier for a service/component
- **CoreIdentity** → Entitlement group used for CrowPilot access control
- **MRT** → Microsoft Red Team — approves access and maintains CrowPilot

## Getting Started

### Step 1: Request Access

Request membership to the **CrowPilot CoreIdentity entitlement group** ("Reader" role is sufficient). Access requests are reviewed and approved by MRT — there is no need to reach out separately for approval.

> **Note:** It can take several hours for CoreIdentity group permissions to propagate.

### Step 2: Download CrowPilot Launcher

Download the launcher script using Azure CLI. The launcher auto-updates on each run, so you always get the latest version:

```powershell
az storage blob download --account-name crowpilotlauncer --container-name launcher --name Launch-RedAICrowpilot.ps1 --file Launch-RedAICrowpilot.ps1 --auth-mode login
```

### Step 3: Verify Installation

Once permissions have propagated, execute the launcher to confirm it works:

```powershell
./Launch-RedAICrowpilot.ps1 --help
```

On startup the agent will verify it is up to date before beginning execution.

## Prerequisites

Before running CrowPilot, the agent **must verify** all prerequisites are met. Use the [prerequisite check script](./scripts/Check-Prerequisites.ps1) to validate the environment.

| Requirement             | Details                                                | Validation                              |
| ----------------------- | ------------------------------------------------------ | --------------------------------------- |
| **Windows OS**          | CrowPilot is currently supported on Windows only       | `$env:OS -eq 'Windows_NT'`              |
| **PowerShell 7+**       | Required runtime                                       | `$PSVersionTable.PSVersion.Major -ge 7` |
| **VPN Connection**      | Must be connected to corporate network                 | Network connectivity check              |
| **Read Access**         | Read access to the code being scanned                  | Path/repo accessibility                 |
| **Local Root Path**     | A local directory for scanning root                    | `Test-Path` validation                  |
| **Azure CLI**           | Required for downloading launcher and ADO scanning     | `Get-Command az` check                  |
| **az azure-devops ext** | ADO extension for az CLI                               | `az extension add --name azure-devops`  |
| **CoreIdentity Access** | CrowPilot CoreIdentity entitlement group (Reader role) | Launcher auth succeeds                  |

> **One-liner prerequisite install**: `az extension add --name azure-devops`

## CLI Reference

```
RedAICrowpilot [options]

Options:
  --rootPath <rootPath>          Root path for the system to operate within
  --agentPath <agentPath>        Agent mode: scan folder path
  --agentSTID <agentSTID>        Agent mode: scan by Service Tree ID
  --agentRepoUrl <agentRepoUrl>  Agent mode: scan ADO Repository URL
  --listExperts                  List available expert agents
  --experts <experts>            Comma-separated list of expert agents
  --continue                     Continue most recently modified session
  --resume <resume>              Resume a specific named session
  --listSessions                 List all available sessions
  -?, -h, --help                 Show help
  --version                      Show version
```

## Core Workflows

### Workflow 1: Environment Setup (First-Time)

Run this workflow if the user has never used CrowPilot before or needs to set up their environment.

1. **Request access** — User must request membership to the CrowPilot CoreIdentity entitlement group (Reader role). Approved by MRT. Permissions can take several hours to propagate.
2. **Check prerequisites** — Run [Check-Prerequisites.ps1](./scripts/Check-Prerequisites.ps1)
3. **Install missing dependencies** — Install Azure DevOps CLI extension if missing
4. **Download the launcher** — Download via Azure CLI:

```powershell
az storage blob download --account-name crowpilotlauncer --container-name launcher --name Launch-RedAICrowpilot.ps1 --file Launch-RedAICrowpilot.ps1 --auth-mode login
```

5. **Verify VPN connectivity** — Confirm corporate network access
6. **Verify installation** — Run `./Launch-RedAICrowpilot.ps1 --help` to confirm the tool works
7. **Report readiness**:

```
✅ CrowPilot Environment Ready
   OS: Windows ✓
   PowerShell: 7.x ✓
   VPN: Connected ✓
   Azure DevOps CLI: Installed ✓
   CoreIdentity Access: Granted ✓
   Launcher: Downloaded ✓
```

### Workflow 2: Agent Mode — Scan Local Path

Use when scanning a local folder (e.g., changed files from a commit).

1. **Validate the target path exists** and is readable
2. **Ask user for expert selection** (optional) — run `--listExperts` to show available experts
3. **Execute the scan**:

```powershell
./Launch-RedAICrowpilot.ps1 --agentPath "<path-to-changed-files>"
```

4. **With specific experts** (if selected):

```powershell
./Launch-RedAICrowpilot.ps1 --agentPath "<path>" --experts "expert1,expert2"
```

5. **Report results** — Summarize findings, severity, and recommended remediation

### Workflow 3: Agent Mode — Scan ADO Repository

Use when scanning an Azure DevOps repository by URL.

1. **Validate the ADO repo URL format**
2. **Ensure Azure DevOps CLI extension is installed**: `az extension add --name azure-devops`
3. **Ensure user is authenticated**: `az login` if needed
4. **Execute the scan**:

```powershell
./Launch-RedAICrowpilot.ps1 --agentRepoUrl "<ado-repo-url>"
```

5. **With specific experts** (if selected):

```powershell
./Launch-RedAICrowpilot.ps1 --agentRepoUrl "<ado-repo-url>" --experts "expert1,expert2"
```

6. **Report results**

### Workflow 4: Agent Mode — Scan by Service Tree ID

Use when scanning all code associated with a Microsoft Service Tree ID.

1. **Validate the Service Tree ID** format
2. **Execute the scan**:

```powershell
./Launch-RedAICrowpilot.ps1 --agentSTID "<service-tree-id>"
```

3. **With specific experts**:

```powershell
./Launch-RedAICrowpilot.ps1 --agentSTID "<stid>" --experts "expert1,expert2"
```

4. **Report results**

### Workflow 5: Interactive Mode

Use when the user wants guided, interactive vulnerability discovery.

1. **Verify rootPath is provided**
2. **Launch interactive mode**:

```powershell
./Launch-RedAICrowpilot.ps1 --rootPath "<root-path>"
```

3. **Guide the user** through the interactive prompts

### Workflow 6: Session Management

Use when continuing or resuming previous scan sessions.

- **List sessions**: `./Launch-RedAICrowpilot.ps1 --listSessions`
- **Continue last session**: `./Launch-RedAICrowpilot.ps1 --continue`
- **Resume specific session**: `./Launch-RedAICrowpilot.ps1 --resume "<session-name>"`

### Workflow 7: Expert Discovery

Use when the user wants to know which vulnerability experts are available.

1. **List experts**: `./Launch-RedAICrowpilot.ps1 --listExperts`
2. **Present findings** with descriptions of each expert's capabilities
3. **Recommend experts** based on the user's target (web app, API, infrastructure, etc.)

## Agent Decision Tree

```
User Request
├── "set up crowpilot" / "check prerequisites"
│   └── → Workflow 1: Environment Setup
├── "scan this folder" / "scan changed files"
│   └── → Workflow 2: Agent Mode — Local Path
├── "scan this ADO repo" / provides ADO URL
│   └── → Workflow 3: Agent Mode — ADO Repository
├── "scan by STID" / provides Service Tree ID
│   └── → Workflow 4: Agent Mode — Service Tree ID
├── "run crowpilot" (no specifics)
│   └── → Workflow 5: Interactive Mode
├── "continue session" / "resume session"
│   └── → Workflow 6: Session Management
├── "list experts" / "what experts are available"
│   └── → Workflow 7: Expert Discovery
└── ambiguous
    └── → Ask user to clarify scan target
```

## What Gets Scanned

- CrowPilot scans **all source code** located under the specified `rootPath`
- Subfolders are included **recursively**
- The agent and its benchmarks are based on **managed code, primarily C#**
- Scanning is performed using the CrowPilot agent backed by **MRT's Foundry**
- For very large codebases, practical limitations apply (see Troubleshooting)

## Scan Output

Identified vulnerabilities are:

1. **Reported to a central inventory** — results are ingested to MRT's central tracking system
2. **Written locally as CSV** — for operator review at `~\.redteamcopilot\vulncsv\`

Output file pattern: `vulnerabilities_{date}_{time}_{id}.csv`

Example ingestion output:

```
Ingestion results: CsvVulnerabilityIngestor → ~\.redteamcopilot\vulncsv\vulnerabilities_20260401_075046_1dc4.csv (Succeeded, 7 records)
```

After a scan, the agent should **read the CSV output** to present findings to the user.

## Result Interpretation

After a scan completes, the agent should:

1. **Locate the CSV output** — Check `~\.redteamcopilot\vulncsv\` for the latest `vulnerabilities_*.csv` file
2. **Summarize findings** — Total vulnerabilities by severity (Critical, High, Medium, Low)
3. **Highlight critical items** — List critical and high-severity findings first
4. **Provide remediation guidance** — Suggest fixes or link to Microsoft security guidance
5. **Recommend next steps** — Re-scan after fixes, escalate critical findings, file bugs

## Troubleshooting

| Issue                      | Possible Cause                                  | Resolution                                                                                                                                                                                  |
| -------------------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CrowPilot not found        | Tool not installed or not in PATH               | Download launcher: `az storage blob download --account-name crowpilotlauncer --container-name launcher --name Launch-RedAICrowpilot.ps1 --file Launch-RedAICrowpilot.ps1 --auth-mode login` |
| VPN error                  | Not connected to corporate network              | Connect to VPN and retry                                                                                                                                                                    |
| ADO auth failure           | Not logged in to Azure CLI                      | Run `az login` and `az devops configure`                                                                                                                                                    |
| Permission denied          | No read access to target code                   | Request access or use a different path                                                                                                                                                      |
| CoreIdentity access denied | Permissions not yet propagated or not requested | Request CrowPilot CoreIdentity group membership (Reader role). Allow several hours for propagation                                                                                          |
| Session not found          | Session name typo or session expired            | Run `--listSessions` to see available sessions                                                                                                                                              |
| PowerShell version error   | PowerShell < 7                                  | Install PowerShell 7: `winget install Microsoft.PowerShell`                                                                                                                                 |
