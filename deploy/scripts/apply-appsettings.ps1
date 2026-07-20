# Applies **code-owned** app-settings from deploy/config/appsettings.<feature>.code.<env>.json
# to the target App Service. `az webapp config appsettings set` upserts by key —
# it does NOT wipe other settings, so this is safe to run alongside Bicep.
#
# Usage:
#   .\apply-appsettings.ps1 -ResourceGroup rg-ra-dev-sin -AppName app-ra-dev-sin-api -EnvName dev -App api
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [Parameter(Mandatory)] [string] $AppName,
    [Parameter(Mandatory)] [string] $EnvName,
    [string] $ConfigDir = (Join-Path $PSScriptRoot '..\config'),
    [string] $App       = 'api',      # feature name — must match filename segment
    [string] $Slot      = ''          # optional deployment slot; empty = production
)

$ErrorActionPreference = 'Stop'
$candidate = Join-Path $ConfigDir "appsettings.$App.code.$EnvName.json"
if (-not (Test-Path $candidate)) {
    Write-Host "No code-owned settings for '$App' in '$EnvName' ($candidate). Skipping."
    return
}
$file = Resolve-Path $candidate
Write-Host "Applying code-owned settings from $file to $AppName$(if($Slot){"/$Slot"})"

$slotArg = if ($Slot) { @('--slot', $Slot) } else { @() }
az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name           $AppName `
    @slotArg `
    --settings       "@$file" `
    --output         none

Write-Host "✅ Code-owned app settings applied."
