<#
.SYNOPSIS
    Validates prerequisites for running RedAICrowpilot.

.DESCRIPTION
    Checks all required dependencies and environment conditions:
    - Windows OS
    - PowerShell 7+
    - VPN connectivity
    - Azure DevOps CLI extension
    - CrowPilot launcher availability
    - Local root path accessibility

.EXAMPLE
    .\Check-Prerequisites.ps1
    .\Check-Prerequisites.ps1 -RootPath "C:\code\my-repo"
    .\Check-Prerequisites.ps1 -InstallMissing
    .\Check-Prerequisites.ps1 -DownloadLauncher -LauncherDestination "C:\tools"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$RootPath,

    [Parameter(Mandatory = $false)]
    [switch]$InstallMissing,

    [Parameter(Mandatory = $false)]
    [switch]$DownloadLauncher,

    [Parameter(Mandatory = $false)]
    [string]$LauncherDestination = "."
)

$script:allPassed = $true
$script:results = @()

function Add-CheckResult {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Details
    )
    $status = if ($Passed) { "PASS" } else { "FAIL" }
    $script:results += [PSCustomObject]@{
        Check   = $Name
        Status  = $status
        Details = $Details
    }
    if (-not $Passed) { $script:allPassed = $false }
}

# --- Check 1: Windows OS ---
$isWindows = $env:OS -eq 'Windows_NT'
Add-CheckResult -Name "Windows OS" -Passed $isWindows -Details $(
    if ($isWindows) { "Running on Windows" } else { "CrowPilot requires Windows. Current OS not supported." }
)

# --- Check 2: PowerShell 7+ ---
$psVersion = $PSVersionTable.PSVersion
$isPwsh7 = $psVersion.Major -ge 7
Add-CheckResult -Name "PowerShell 7+" -Passed $isPwsh7 -Details $(
    if ($isPwsh7) { "PowerShell $($psVersion)" } else { "PowerShell $($psVersion) detected. Install PS7: winget install Microsoft.PowerShell" }
)

# --- Check 3: Azure CLI ---
$azInstalled = $null -ne (Get-Command az -ErrorAction SilentlyContinue)
Add-CheckResult -Name "Azure CLI" -Passed $azInstalled -Details $(
    if ($azInstalled) { "Azure CLI found" } else { "Azure CLI not found. Install: winget install Microsoft.AzureCLI" }
)

# --- Check 4: Azure DevOps CLI extension ---
$adoExtInstalled = $false
if ($azInstalled) {
    try {
        $extensions = az extension list 2>$null | ConvertFrom-Json
        $adoExtInstalled = ($extensions | Where-Object { $_.name -eq 'azure-devops' }).Count -gt 0
    }
    catch {
        $adoExtInstalled = $false
    }
}
Add-CheckResult -Name "Azure DevOps CLI Extension" -Passed $adoExtInstalled -Details $(
    if ($adoExtInstalled) { "azure-devops extension installed" }
    elseif (-not $azInstalled) { "Skipped - Azure CLI not installed" }
    else { "azure-devops extension not found. Install: az extension add --name azure-devops" }
)

if (-not $adoExtInstalled -and $azInstalled -and $InstallMissing) {
    Write-Host "Installing Azure DevOps CLI extension..." -ForegroundColor Yellow
    az extension add --name azure-devops
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Azure DevOps CLI extension installed successfully." -ForegroundColor Green
    }
    else {
        Write-Host "Failed to install Azure DevOps CLI extension." -ForegroundColor Red
    }
}

# --- Check 5: VPN Connectivity ---
# Attempt to resolve a known internal Microsoft hostname as a proxy for VPN status
$vpnConnected = $false
try {
    $dnsResult = Resolve-DnsName -Name "microsoft.com" -ErrorAction SilentlyContinue
    $vpnConnected = $null -ne $dnsResult
}
catch {
    $vpnConnected = $false
}
Add-CheckResult -Name "Network Connectivity" -Passed $vpnConnected -Details $(
    if ($vpnConnected) { "Network reachable" } else { "Cannot resolve internal hosts. Ensure VPN is connected." }
)

# --- Check 6: CrowPilot Launcher ---
$launcherPath = Join-Path $LauncherDestination "Launch-RedAICrowpilot.ps1"
$launcherExists = Test-Path -Path $launcherPath
Add-CheckResult -Name "CrowPilot Launcher" -Passed $launcherExists -Details $(
    if ($launcherExists) { "Launcher found: $launcherPath" }
    else { "Launcher not found. Download: az storage blob download --account-name crowpilotlauncer --container-name launcher --name Launch-RedAICrowpilot.ps1 --file Launch-RedAICrowpilot.ps1 --auth-mode login" }
)

if (-not $launcherExists -and $DownloadLauncher -and $azInstalled) {
    Write-Host "Downloading CrowPilot launcher..." -ForegroundColor Yellow
    $destFile = Join-Path $LauncherDestination "Launch-RedAICrowpilot.ps1"
    az storage blob download --account-name crowpilotlauncer --container-name launcher --name Launch-RedAICrowpilot.ps1 --file $destFile --auth-mode login
    if ($LASTEXITCODE -eq 0) {
        Write-Host "CrowPilot launcher downloaded to $destFile" -ForegroundColor Green
    }
    else {
        Write-Host "Failed to download launcher. Ensure you have CoreIdentity access and are connected to VPN." -ForegroundColor Red
    }
}

# --- Check 7: Root Path (optional) ---
if ($RootPath) {
    $pathExists = Test-Path -Path $RootPath
    Add-CheckResult -Name "Root Path" -Passed $pathExists -Details $(
        if ($pathExists) { "Path exists: $RootPath" } else { "Path not found: $RootPath" }
    )
}

# --- Report ---
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " RedAI CrowPilot Prerequisite Check" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

foreach ($r in $script:results) {
    $color = if ($r.Status -eq "PASS") { "Green" } else { "Red" }
    $icon = if ($r.Status -eq "PASS") { "[OK]" } else { "[!!]" }
    Write-Host "  $icon $($r.Check): $($r.Details)" -ForegroundColor $color
}

Write-Host ""
if ($script:allPassed) {
    Write-Host "  All checks passed. CrowPilot is ready to run." -ForegroundColor Green
}
else {
    Write-Host "  Some checks failed. Please resolve the issues above before running CrowPilot." -ForegroundColor Red
}
Write-Host ""

# Return structured result for programmatic consumption
return [PSCustomObject]@{
    AllPassed = $script:allPassed
    Results   = $script:results
}
