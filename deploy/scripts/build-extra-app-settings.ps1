#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build the Bicep `extraAppSettingsMap` object for an environment.

.DESCRIPTION
    For every feature with an infra or code settings file in this repo,
    produce { <feature>: [ {name, value}, ... ] }.

    Additionally, to prevent Bicep's `siteConfig.appSettings` collection
    replace from wiping settings that are owned by the code pipeline
    (feature flags, `Data__IsMocked`, etc.), we look up the corresponding
    live app service and copy any code-owned keys' current values into the
    map. On cold start (app doesn't exist yet), we seed from the code file.

.PARAMETER TargetEnv
    Environment name (dev, test, uat, prod).

.PARAMETER ResourceGroup
    Resource group containing the app services (e.g. rg-ra-dev-sin).
    Optional — if empty, seed values are always used from the code file.

.PARAMETER AppNameMap
    Optional hashtable/JSON string mapping feature -> app service name
    (e.g. '{"api":"app-ra-dev-sin-api","web":"app-ra-dev-sin-web"}').
    Required if -ResourceGroup is provided.

.PARAMETER OutFile
    Where to write the final JSON. Defaults to stdout.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $TargetEnv,
    [string] $ResourceGroup = '',
    [string] $AppNameMap    = '',
    [string] $OutFile       = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cfgDir   = Join-Path $repoRoot 'deploy\config'

$nameMap = @{}
if ($AppNameMap) { $nameMap = $AppNameMap | ConvertFrom-Json -AsHashtable }

$map = @{}
$features = Get-ChildItem $cfgDir -Filter "appsettings.*.$TargetEnv.json" |
    Where-Object BaseName -Match "\.(infra|code)\.$TargetEnv$" |
    ForEach-Object { $_.BaseName -replace "^appsettings\.", "" -replace "\.(infra|code)\.$TargetEnv$", "" } |
    Sort-Object -Unique
$features | ForEach-Object {
    $feature = $_
    $infraFile = Join-Path $cfgDir "appsettings.$feature.infra.$TargetEnv.json"
    $infraSettings = @()
    if (Test-Path $infraFile) {
        Write-Host "[$feature] infra settings from $(Split-Path $infraFile -Leaf)"
        $infraSettings = @(Get-Content $infraFile -Raw | ConvertFrom-Json)
    }

    # Determine code-owned keys for this feature: names from the paired code file (if any).
    $codeFile = Join-Path $cfgDir "appsettings.$feature.code.$TargetEnv.json"
    $codeOwned = @{}
    if (Test-Path $codeFile) {
        $seedList = @(Get-Content $codeFile -Raw | ConvertFrom-Json)
        foreach ($s in $seedList) { $codeOwned[$s.name] = $s.value }  # seed values
    }

    # If we have a live app, prefer its current values for code-owned keys.
    if ($ResourceGroup -and $nameMap.ContainsKey($feature)) {
        $appName = $nameMap[$feature]
        Write-Host "[$feature] querying live settings on $appName ..."
        $liveJson = az webapp config appsettings list -g $ResourceGroup -n $appName -o json
        if ($LASTEXITCODE -ne 0) {
            throw "Cannot read settings for '$appName'; refusing to replace live code-owned values with seeds."
        }
        $live = $liveJson | ConvertFrom-Json
        foreach ($k in @($codeOwned.Keys)) {
            $hit = $live | Where-Object name -eq $k | Select-Object -First 1
            if ($hit) {
                Write-Host "  preserving live value of $k"
                $codeOwned[$k] = $hit.value
            } else {
                Write-Host "  using seed value for $k"
            }
        }
    }

    # Code-owned values take precedence without duplicate App Service keys.
    $settingsByName = [ordered]@{}
    foreach ($setting in $infraSettings) { $settingsByName[$setting.name] = $setting.value }
    foreach ($k in $codeOwned.Keys) { $settingsByName[$k] = $codeOwned[$k] }
    $map[$feature] = @($settingsByName.GetEnumerator() | ForEach-Object {
        @{ name = $_.Key; value = $_.Value }
    })
}

$json = $map | ConvertTo-Json -Depth 10 -Compress
if ($OutFile) { $json | Set-Content -NoNewline $OutFile }
else          { Write-Output $json }
