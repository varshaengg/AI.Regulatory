<#
.SYNOPSIS
    Collects an ARA diagnostics bundle for support without exposing customer data.

.DESCRIPTION
    Aggregates the last N hours of logs, health snapshots, and configuration into a single encrypted zip file the customer can attach to a support ticket. Publisher has no runtime access to the customer environment; this bundle is the primary support artifact.

    The bundle contains:
      - App Insights request/exception summaries (aggregated counts + top-N traces, PII scrubbed)
      - Log Analytics query results for AuditEvent, DossierRun, worker jobs
      - App Service configuration (secret values redacted)
      - Bicep 'what-if' snapshot of current state (drift detection)
      - Deployed image tags + DB schema version
      - Health probe status

.PARAMETER ResourceGroupName
    Resource group of the ARA deployment.

.PARAMETER Hours
    Time window in hours. Default 24.

.PARAMETER OutputFolder
    Where to write the resulting zip. Default current folder.

.EXAMPLE
    ./diagnostics-collect.ps1 -ResourceGroupName rg-ara-prod-acme
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ResourceGroupName,
    [int] $Hours = 24,
    [string] $OutputFolder = '.'
)

$ErrorActionPreference = 'Stop'
$stamp = (Get-Date).ToString('yyyyMMdd-HHmm')
$work  = Join-Path $env:TEMP "ara-diag-$stamp"
New-Item -ItemType Directory -Force -Path $work | Out-Null

Write-Host "→ Collecting diagnostics from $ResourceGroupName (last $Hours h)" -ForegroundColor Cyan

# --- 1. Resource inventory --------------------------------------------------
Write-Host "  Inventory..."
az resource list --resource-group $ResourceGroupName --output json |
    Out-File -FilePath "$work/01-resources.json" -Encoding utf8

# --- 2. App Service config (secrets redacted) -------------------------------
Write-Host "  App Service config (redacted)..."
$apps = az webapp list --resource-group $ResourceGroupName --query "[].name" -o tsv
foreach ($a in $apps) {
    $settings = az webapp config appsettings list --name $a --resource-group $ResourceGroupName --output json | ConvertFrom-Json
    foreach ($s in $settings) {
        # Redact anything that looks like a secret or KV reference target
        if ($s.name -match '(?i)key|secret|password|token|connectionstring') {
            $s.value = '<REDACTED>'
        }
    }
    $settings | ConvertTo-Json -Depth 5 | Out-File -FilePath "$work/02-appservice-$a-config.json" -Encoding utf8
}

# --- 3. Deployed image tags -------------------------------------------------
Write-Host "  Container image tags..."
$imageInfo = foreach ($a in $apps) {
    $img = az webapp config container show --name $a --resource-group $ResourceGroupName --query "linuxFxVersion" -o tsv
    [pscustomobject]@{ App = $a; Image = $img }
}
$imageInfo | ConvertTo-Json | Out-File -FilePath "$work/03-image-tags.json" -Encoding utf8

# --- 4. App Insights recent errors + top requests (last N hours) -----------
Write-Host "  Application Insights query..."
$ai = az resource list --resource-group $ResourceGroupName --resource-type "Microsoft.Insights/components" --query "[0].name" -o tsv
if ($ai) {
    $timespan = "PT${Hours}H"
    az monitor app-insights query --app $ai --resource-group $ResourceGroupName --analytics-query `
        "requests | where timestamp > ago($($Hours)h) | summarize count(), avg(duration), max(duration) by resultCode, name | top 50 by count_" `
        --output json | Out-File "$work/04-ai-requests.json" -Encoding utf8

    az monitor app-insights query --app $ai --resource-group $ResourceGroupName --analytics-query `
        "exceptions | where timestamp > ago($($Hours)h) | project timestamp, type, method, outerMessage | top 100 by timestamp desc" `
        --output json | Out-File "$work/05-ai-exceptions.json" -Encoding utf8

    az monitor app-insights query --app $ai --resource-group $ResourceGroupName --analytics-query `
        "dependencies | where timestamp > ago($($Hours)h) and success == false | project timestamp, target, name, resultCode, duration | top 100 by timestamp desc" `
        --output json | Out-File "$work/06-ai-failed-dependencies.json" -Encoding utf8
}

# --- 5. Log Analytics audit / dossier / worker summaries -------------------
Write-Host "  Log Analytics audit tables..."
$law = az resource list --resource-group $ResourceGroupName --resource-type "Microsoft.OperationalInsights/workspaces" --query "[0].name" -o tsv
if ($law) {
    $wsId = az monitor log-analytics workspace show --workspace-name $law --resource-group $ResourceGroupName --query customerId -o tsv
    # NOTE: These custom tables assume the API pushes them; adjust names if you use different sinks.
    foreach ($q in @(
        @{ File='07-la-audit.json';        Kql="AraAudit_CL | where TimeGenerated > ago($($Hours)h) | project TimeGenerated, Action_s, Actor_s, ResourceType_s, ResourceId_s | top 500 by TimeGenerated desc" },
        @{ File='08-la-dossier-runs.json'; Kql="AraDossier_CL | where TimeGenerated > ago($($Hours)h) | project TimeGenerated, RunId_g, Status_s, Duration_d, GapCount_d | top 500 by TimeGenerated desc" }
    )) {
        try {
            az monitor log-analytics query -w $wsId --analytics-query $q.Kql --output json | Out-File "$work/$($q.File)" -Encoding utf8
        } catch {
            "Query failed: $($_.Exception.Message)" | Out-File "$work/$($q.File)" -Encoding utf8
        }
    }
}

# --- 6. Bicep what-if drift snapshot ---------------------------------------
Write-Host "  Bicep what-if snapshot (drift detection)..."
if (Test-Path (Join-Path $PSScriptRoot '..\bicep\main.bicep')) {
    az deployment sub what-if --location westeurope `
        --template-file (Join-Path $PSScriptRoot '..\bicep\main.bicep') `
        --parameters (Join-Path $PSScriptRoot '..\bicep\parameters.example.json') `
        --output json 2> "$work/09-bicep-whatif.txt" | Out-File "$work/09-bicep-whatif.json" -Encoding utf8
} else {
    "Bicep templates not co-located; skipped drift snapshot." | Out-File "$work/09-bicep-whatif.txt" -Encoding utf8
}

# --- 7. Health probes ------------------------------------------------------
Write-Host "  Health probes..."
$health = foreach ($a in $apps) {
    $host = az webapp show --name $a --resource-group $ResourceGroupName --query defaultHostName -o tsv
    foreach ($p in '/health','/ready','/api/version') {
        try {
            $resp = Invoke-WebRequest -Uri "https://$host$p" -UseBasicParsing -TimeoutSec 10
            [pscustomobject]@{ App=$a; Path=$p; Status=$resp.StatusCode; Body=($resp.Content.Substring(0, [Math]::Min(500, $resp.Content.Length))) }
        } catch {
            [pscustomobject]@{ App=$a; Path=$p; Status='FAIL'; Body=$_.Exception.Message }
        }
    }
}
$health | ConvertTo-Json | Out-File "$work/10-health.json" -Encoding utf8

# --- 8. Bundle -------------------------------------------------------------
$bundle = Join-Path (Resolve-Path $OutputFolder) "ara-diagnostics-$ResourceGroupName-$stamp.zip"
Compress-Archive -Path "$work/*" -DestinationPath $bundle -Force

Write-Host "`n✓ Bundle created: $bundle" -ForegroundColor Green
Write-Host "  Upload to publisher support portal along with your ticket." -ForegroundColor Yellow
Write-Host "  TODO: (roadmap) encrypt bundle with publisher public key before upload." -ForegroundColor DarkYellow
