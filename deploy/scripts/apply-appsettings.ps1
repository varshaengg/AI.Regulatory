# Applies API app-settings from deploy/config/appsettings.api.<env>.json to the target App Service.
# Idempotent — safe to run every deploy. Values override anything currently set.
#
# Usage:
#   .\apply-appsettings.ps1 -ResourceGroup rg-ra-dev-sin -AppName app-ra-dev-sin-api -EnvName dev
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [Parameter(Mandatory)] [string] $AppName,
    [Parameter(Mandatory)] [string] $EnvName,
    [string] $ConfigDir = (Join-Path $PSScriptRoot '..\config'),
    [string] $App       = 'api',      # 'api' | 'web'
    [string] $Slot      = ''          # optional deployment slot; empty = production
)

$ErrorActionPreference = 'Stop'
$file = Resolve-Path (Join-Path $ConfigDir "appsettings.$App.$EnvName.json")
Write-Host "Applying settings from $file to $AppName$(if($Slot){"/$Slot"})"

$slotArg = if ($Slot) { @('--slot', $Slot) } else { @() }
az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name           $AppName `
    @slotArg `
    --settings       "@$file" `
    --output         none

Write-Host "✅ App settings applied."
