<#
.SYNOPSIS
    Uploads the shipped CTD template catalog into the customer's Blob storage.

.DESCRIPTION
    Reads a folder of CTD template files (Word/YAML) shipped in the deploy bundle and uploads them into the ara-catalog container in the customer's storage account. Idempotent — computes SHA-256 per file and skips uploads whose content-addressed name already exists.

.PARAMETER StorageAccountName
    Name of the target customer storage account.

.PARAMETER ContainerName
    Blob container. Defaults to 'ara-catalog'.

.PARAMETER TemplatesSourcePath
    Local folder containing template files (each file is one CTD template version).

.EXAMPLE
    ./seed-templates.ps1 -StorageAccountName staraprodacme -TemplatesSourcePath ./catalog-seed/
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $StorageAccountName,
    [string] $ContainerName       = 'ara-catalog',
    [Parameter(Mandatory)] [string] $TemplatesSourcePath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $TemplatesSourcePath)) {
    throw "Templates source path does not exist: $TemplatesSourcePath"
}

Write-Host "→ Authenticating to storage account: $StorageAccountName" -ForegroundColor Cyan
$ctx = New-AzStorageContext -StorageAccountName $StorageAccountName -UseConnectedAccount

# Ensure container exists (idempotent)
$container = Get-AzStorageContainer -Context $ctx -Name $ContainerName -ErrorAction SilentlyContinue
if (-not $container) {
    Write-Host "  Creating container $ContainerName"
    New-AzStorageContainer -Context $ctx -Name $ContainerName -Permission Off | Out-Null
}

$files = Get-ChildItem -Path $TemplatesSourcePath -File -Recurse
if (-not $files) {
    Write-Warning "No template files found in $TemplatesSourcePath — nothing to seed."
    return
}

$uploaded = 0; $skipped = 0
foreach ($f in $files) {
    $hash = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $blobName = "$hash/$($f.Name)"

    $existing = Get-AzStorageBlob -Container $ContainerName -Context $ctx -Blob $blobName -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "  ⏭  $($f.Name) — already present (sha256=$hash)"
        $skipped++
        continue
    }

    Write-Host "  ⬆  Uploading $($f.Name) (sha256=$hash)"
    Set-AzStorageBlobContent -File $f.FullName -Container $ContainerName -Blob $blobName -Context $ctx `
        -Metadata @{
            originalName = $f.Name
            uploadedAt   = (Get-Date).ToString('o')
            sourceBundle = 'seed-templates.ps1'
        } | Out-Null
    $uploaded++
}

Write-Host "`n✓ Catalog seed complete: $uploaded uploaded, $skipped skipped." -ForegroundColor Green
