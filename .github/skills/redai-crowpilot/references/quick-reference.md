# RedAI CrowPilot Quick Reference

## Tool Overview

RedAICrowpilot is an AI-powered vulnerability discovery tool for red team operators. It analyzes source code to identify security vulnerabilities, providing findings with severity ratings and remediation guidance. Scanning is backed by MRT's Foundry and primarily targets managed code (C#).

## Getting Started

```powershell
# 1. Request CrowPilot CoreIdentity entitlement group membership (Reader role)
#    Approved by MRT — allow several hours for permission propagation

# 2. Download the launcher (auto-updates on each run)
az storage blob download --account-name crowpilotlauncer --container-name launcher --name Launch-RedAICrowpilot.ps1 --file Launch-RedAICrowpilot.ps1 --auth-mode login

# 3. Verify it works
./Launch-RedAICrowpilot.ps1 --help
```

## What Gets Scanned

- All source code under the specified `rootPath` (recursive)
- Primarily managed code (C#) based on agent benchmarks
- Results reported to central inventory + local CSV at `~\.redteamcopilot\vulncsv\`

## Supported Scan Modes

| Mode | Flag | Description |
|------|------|-------------|
| **Agent — Local Path** | `--agentPath <path>` | Scan a local folder (e.g., changed files from a commit) |
| **Agent — Service Tree ID** | `--agentSTID <stid>` | Scan all code associated with a Microsoft Service Tree ID |
| **Agent — ADO Repo** | `--agentRepoUrl <url>` | Scan an Azure DevOps repository by URL |
| **Interactive** | `--rootPath <path>` | Guided interactive scanning session |

## Common Usage Patterns

### Scan changed files from a PR/commit

```powershell
./Launch-RedAICrowpilot.ps1 --agentPath "C:\code\my-repo\changed-files"
```

### Scan an ADO repository

```powershell
./Launch-RedAICrowpilot.ps1 --agentRepoUrl "https://dev.azure.com/org/project/_git/repo"
```

### Scan with specific experts

```powershell
# First, list available experts
./Launch-RedAICrowpilot.ps1 --listExperts

# Then run with selected experts
./Launch-RedAICrowpilot.ps1 --agentPath "C:\code\changes" --experts "web-vuln,api-security"
```

### Session management

```powershell
# List all saved sessions
./Launch-RedAICrowpilot.ps1 --listSessions

# Continue the most recent session
./Launch-RedAICrowpilot.ps1 --continue

# Resume a specific session by name
./Launch-RedAICrowpilot.ps1 --resume "my-scan-session"
```

## Expert Agents

CrowPilot includes specialized expert agents for different vulnerability domains. Use `--listExperts` to see the current list. You can combine multiple experts with a comma-separated list via `--experts`.

## Severity Levels

| Level | Description |
|-------|-------------|
| **Critical** | Exploitable vulnerabilities requiring immediate attention |
| **High** | Significant security risks that should be addressed promptly |
| **Medium** | Moderate risks that should be planned for remediation |
| **Low** | Minor issues or informational findings |

## Environment Requirements

- Windows OS
- PowerShell 7+
- VPN connection to corporate network
- Azure CLI with azure-devops extension (`az extension add --name azure-devops`)
- CrowPilot CoreIdentity entitlement group membership (Reader role)
- Read access to target code

## Scan Output

Vulnerabilities are written to: `~\.redteamcopilot\vulncsv\vulnerabilities_{date}_{time}_{id}.csv`
