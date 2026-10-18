$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'build-extra-app-settings.ps1'
$outFile = Join-Path ([IO.Path]::GetTempPath()) "ara-settings-test-$([guid]::NewGuid()).json"

function Assert-UniqueSettings($settings) {
    foreach ($feature in $settings.Keys) {
        $duplicates = @($settings[$feature] | Group-Object name | Where-Object Count -gt 1)
        if ($duplicates.Count) {
            throw "Duplicate settings for '$feature': $($duplicates.Name -join ', ')."
        }
    }
}

function az {
    $global:LASTEXITCODE = [int]$env:ARA_SETTINGS_TEST_EXIT_CODE
    if ($global:LASTEXITCODE -eq 0) {
        '[{"name":"EntraId__Audience","value":"api://test-live-audience"}]'
    }
}

try {
    & $scriptPath -TargetEnv dev -OutFile $outFile
    $settings = Get-Content $outFile -Raw | ConvertFrom-Json -AsHashtable
    Assert-UniqueSettings $settings
    if (@($settings.api | Where-Object name -eq 'EntraId__Audience').Count -ne 1) {
        throw 'Cold-start settings must contain exactly one audience.'
    }

    $env:ARA_SETTINGS_TEST_EXIT_CODE = '0'
    $liveArgs = @{
        TargetEnv = 'dev'
        ResourceGroup = 'rg-test'
        AppNameMap = '{"api":"app-test-api"}'
        OutFile = $outFile
    }
    & $scriptPath @liveArgs
    $settings = Get-Content $outFile -Raw | ConvertFrom-Json -AsHashtable
    Assert-UniqueSettings $settings
    $audience = $settings.api | Where-Object name -eq 'EntraId__Audience'
    if ($audience.value -ne 'api://test-live-audience') {
        throw 'The live code-owned audience must override the infra seed.'
    }
    if ($settings.web.Count -ne 0) {
        throw 'Empty web settings must remain a JSON array.'
    }

    $env:ARA_SETTINGS_TEST_EXIT_CODE = '1'
    $readFailed = $false
    try {
        & $scriptPath @liveArgs
    } catch {
        if ($_.Exception.Message -notlike '*refusing to replace live code-owned values*') { throw }
        $readFailed = $true
    }
    if (-not $readFailed) {
        throw 'An Azure read failure must stop the settings replacement.'
    }
    Write-Host 'PASS: unique cold-start keys, live ownership precedence, empty arrays, and fail-closed reads.'
} finally {
    if (Test-Path $outFile) { Remove-Item -LiteralPath $outFile }
    Remove-Item Env:\ARA_SETTINGS_TEST_EXIT_CODE -ErrorAction SilentlyContinue
}
