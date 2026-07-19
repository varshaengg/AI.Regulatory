<#
.SYNOPSIS
    Wrapper to invoke RedAICrowpilot with the correct parameters.

.DESCRIPTION
    Simplifies invoking RedAICrowpilot by providing named parameters and
    handling common validation before launching the tool.

.PARAMETER Mode
    The scan mode: AgentPath, AgentSTID, AgentRepoUrl, Interactive, Continue, Resume, ListExperts, ListSessions

.PARAMETER Target
    The target value (path, STID, URL, or session name depending on Mode)

.PARAMETER RootPath
    The root path for interactive mode

.PARAMETER Experts
    Comma-separated list of expert agents to run

.PARAMETER LaunchScript
    Path to the Launch-RedAICrowpilot.ps1 script

.EXAMPLE
    .\Invoke-CrowPilot.ps1 -Mode AgentPath -Target "C:\code\changes" -LaunchScript ".\Launch-RedAICrowpilot.ps1"
    .\Invoke-CrowPilot.ps1 -Mode AgentRepoUrl -Target "https://dev.azure.com/org/project/_git/repo" -Experts "web,api"
    .\Invoke-CrowPilot.ps1 -Mode ListExperts
    .\Invoke-CrowPilot.ps1 -Mode Continue
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("AgentPath", "AgentSTID", "AgentRepoUrl", "Interactive", "Continue", "Resume", "ListExperts", "ListSessions")]
    [string]$Mode,

    [Parameter(Mandatory = $false)]
    [string]$Target,

    [Parameter(Mandatory = $false)]
    [string]$RootPath,

    [Parameter(Mandatory = $false)]
    [string]$Experts,

    [Parameter(Mandatory = $false)]
    [string]$LaunchScript = ".\Launch-RedAICrowpilot.ps1"
)

# Validate launch script exists
if (-not (Test-Path $LaunchScript)) {
    Write-Error "Launch script not found at: $LaunchScript"
    Write-Host "Please provide the correct path to Launch-RedAICrowpilot.ps1 using -LaunchScript parameter." -ForegroundColor Yellow
    return
}

# Build arguments
$arguments = @()

switch ($Mode) {
    "AgentPath" {
        if (-not $Target) {
            Write-Error "AgentPath mode requires -Target parameter (folder path to scan)"
            return
        }
        if (-not (Test-Path $Target)) {
            Write-Error "Target path does not exist: $Target"
            return
        }
        $arguments += "--agentPath", "`"$Target`""
    }
    "AgentSTID" {
        if (-not $Target) {
            Write-Error "AgentSTID mode requires -Target parameter (Service Tree ID)"
            return
        }
        $arguments += "--agentSTID", "`"$Target`""
    }
    "AgentRepoUrl" {
        if (-not $Target) {
            Write-Error "AgentRepoUrl mode requires -Target parameter (ADO Repository URL)"
            return
        }
        # Basic URL validation
        if ($Target -notmatch '^https?://') {
            Write-Error "Target does not appear to be a valid URL: $Target"
            return
        }
        $arguments += "--agentRepoUrl", "`"$Target`""
    }
    "Interactive" {
        if (-not $RootPath) {
            Write-Error "Interactive mode requires -RootPath parameter"
            return
        }
        if (-not (Test-Path $RootPath)) {
            Write-Error "Root path does not exist: $RootPath"
            return
        }
        $arguments += "--rootPath", "`"$RootPath`""
    }
    "Continue" {
        $arguments += "--continue"
    }
    "Resume" {
        if (-not $Target) {
            Write-Error "Resume mode requires -Target parameter (session name)"
            return
        }
        $arguments += "--resume", "`"$Target`""
    }
    "ListExperts" {
        $arguments += "--listExperts"
    }
    "ListSessions" {
        $arguments += "--listSessions"
    }
}

# Add experts if specified (applicable to agent modes)
if ($Experts -and $Mode -in @("AgentPath", "AgentSTID", "AgentRepoUrl")) {
    $arguments += "--experts", "`"$Experts`""
}

# Display command being executed
$fullCommand = "$LaunchScript $($arguments -join ' ')"
Write-Host ""
Write-Host "Executing CrowPilot:" -ForegroundColor Cyan
Write-Host "  $fullCommand" -ForegroundColor White
Write-Host ""

# Execute
& $LaunchScript @arguments
