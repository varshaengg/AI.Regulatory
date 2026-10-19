[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BaseUrl,
    [string[]] $Paths = @('/health/live', '/health/ready'),
    [ValidateRange(1, 30)] [int] $Attempts = 12,
    [ValidateRange(1, 60)] [int] $DelaySeconds = 10
)

$ErrorActionPreference = 'Stop'
foreach ($path in $Paths) {
    $healthy = $false
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$($BaseUrl.TrimEnd('/'))$path" `
                -SkipHttpErrorCheck -TimeoutSec 30
            $lastResult = "HTTP $([int]$response.StatusCode)"
            $healthy = [int]$response.StatusCode -eq 200
        } catch [System.Net.Http.HttpRequestException] {
            $lastResult = 'Connection failed'
        } catch [System.Threading.Tasks.TaskCanceledException] {
            $lastResult = 'Request timed out'
        }
        Write-Host "$path attempt $attempt/$Attempts : $lastResult"
        if ($healthy) { break }
        if ($attempt -lt $Attempts) { Start-Sleep -Seconds $DelaySeconds }
    }
    if (-not $healthy) { throw "Health check failed for $path after $Attempts attempts ($lastResult)." }
}
