<#
.SYNOPSIS
    Creates the two Entra ID app registrations required by ARA in the customer's tenant.

.DESCRIPTION
    Idempotent PowerShell script. Run ONCE by a Global Administrator (or Application Administrator + Privileged Role Administrator) in the customer's Entra tenant during first install.

    Creates:
      - ARA-API     : ASP.NET Core API — exposes access_as_user scope + App Roles
      - ARA-Client  : WPF client — public client, WAM redirect URIs
    Grants API permissions and prints the ClientIds to paste into parameters.<env>.json.

.PREREQUISITES
    Microsoft.Graph PowerShell module (Install-Module Microsoft.Graph)
    Signed in as a user with sufficient privilege (Connect-MgGraph -Scopes 'Application.ReadWrite.All','DelegatedPermissionGrant.ReadWrite.All','AppRoleAssignment.ReadWrite.All')

.EXAMPLE
    ./bootstrap-appreg.ps1 -TenantId 11111111-2222-3333-4444-555555555555 -DisplayNamePrefix 'ARA-Prod'
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $TenantId,
    [string] $DisplayNamePrefix = 'ARA',
    [string] $OutputFile        = './appreg-output.json'
)

$ErrorActionPreference = 'Stop'

# --- 0. Pre-flight ---------------------------------------------------------
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Applications)) {
    throw "Microsoft.Graph PowerShell module not installed. Run:  Install-Module Microsoft.Graph -Scope CurrentUser"
}
Import-Module Microsoft.Graph.Applications
Connect-MgGraph -TenantId $TenantId -Scopes @(
    'Application.ReadWrite.All',
    'DelegatedPermissionGrant.ReadWrite.All',
    'AppRoleAssignment.ReadWrite.All'
) -NoWelcome

$graphSpAppId = '00000003-0000-0000-c000-000000000000'   # Microsoft Graph service principal
$graphSp = Get-MgServicePrincipal -Filter "appId eq '$graphSpAppId'"

# --- 1. ARA-API registration ----------------------------------------------
$apiName = "$DisplayNamePrefix-API"
Write-Host "→ Creating/updating app registration: $apiName" -ForegroundColor Cyan

$apiApp = Get-MgApplication -Filter "displayName eq '$apiName'" -Top 1
if (-not $apiApp) {
    $apiApp = New-MgApplication -DisplayName $apiName -SignInAudience 'AzureADMyOrg'
}
$apiClientId = $apiApp.AppId

# Identifier URI
$identifierUri = "api://$apiClientId"
if ($apiApp.IdentifierUris -notcontains $identifierUri) {
    Update-MgApplication -ApplicationId $apiApp.Id -IdentifierUris @($identifierUri)
}

# Expose an API — access_as_user scope
$oauth2Scope = @{
    Id                      = [Guid]::NewGuid().ToString()
    AdminConsentDescription = 'Allows the ARA client to call the ARA API on behalf of the signed-in user.'
    AdminConsentDisplayName = 'Access ARA on behalf of the signed-in user'
    IsEnabled               = $true
    Type                    = 'User'
    UserConsentDescription  = 'Allows the ARA client to call the ARA API on your behalf.'
    UserConsentDisplayName  = 'Access ARA on your behalf'
    Value                   = 'access_as_user'
}

# App roles (Regulatory RBAC)
$appRoles = @(
    @{ Id=[Guid]::NewGuid().ToString(); AllowedMemberTypes=@('User'); Description='Regulatory Executive';   DisplayName='Regulatory Executive';   IsEnabled=$true; Value='RegulatoryExecutive' },
    @{ Id=[Guid]::NewGuid().ToString(); AllowedMemberTypes=@('User'); Description='Regulatory Manager';     DisplayName='Regulatory Manager';     IsEnabled=$true; Value='RegulatoryManager' },
    @{ Id=[Guid]::NewGuid().ToString(); AllowedMemberTypes=@('User'); Description='ARA Administrator';      DisplayName='Administrator';          IsEnabled=$true; Value='Administrator' },
    @{ Id=[Guid]::NewGuid().ToString(); AllowedMemberTypes=@('User'); Description='Read-only Auditor';      DisplayName='Auditor';                IsEnabled=$true; Value='Auditor' }
)

Update-MgApplication -ApplicationId $apiApp.Id `
    -Api @{ Oauth2PermissionScopes = @($oauth2Scope) } `
    -AppRoles $appRoles

# Delegated Graph permissions on the API
$delegatedPerms = @('User.Read','Sites.Read.All','Files.Read.All','GroupMember.Read.All')
$resourceAccess = foreach ($permName in $delegatedPerms) {
    $scope = $graphSp.Oauth2PermissionScopes | Where-Object { $_.Value -eq $permName } | Select-Object -First 1
    if (-not $scope) { throw "Graph delegated permission not found: $permName" }
    @{ Id = $scope.Id; Type = 'Scope' }
}
$requiredResourceAccess = @{
    ResourceAppId  = $graphSpAppId
    ResourceAccess = @($resourceAccess)
}
Update-MgApplication -ApplicationId $apiApp.Id -RequiredResourceAccess @($requiredResourceAccess)

# Ensure service principal exists
$apiSp = Get-MgServicePrincipal -Filter "appId eq '$apiClientId'" -Top 1
if (-not $apiSp) { $apiSp = New-MgServicePrincipal -AppId $apiClientId }

# --- 2. ARA-Client registration -------------------------------------------
$clientName = "$DisplayNamePrefix-Client"
Write-Host "→ Creating/updating app registration: $clientName" -ForegroundColor Cyan

$clientApp = Get-MgApplication -Filter "displayName eq '$clientName'" -Top 1
if (-not $clientApp) {
    $clientApp = New-MgApplication -DisplayName $clientName -SignInAudience 'AzureADMyOrg' -IsFallbackPublicClient:$true
}
$clientClientId = $clientApp.AppId

# Public-client redirect URIs (WAM broker + nativeclient)
$redirects = @(
    'https://login.microsoftonline.com/common/oauth2/nativeclient',
    "ms-appx-web://microsoft.aad.brokerplugin/$clientClientId"
)
Update-MgApplication -ApplicationId $clientApp.Id `
    -PublicClient @{ RedirectUris = $redirects } `
    -IsFallbackPublicClient:$true

# Client → API scope permission
$clientReqAccess = @{
    ResourceAppId  = $apiClientId
    ResourceAccess = @( @{ Id = $oauth2Scope.Id; Type = 'Scope' } )
}
Update-MgApplication -ApplicationId $clientApp.Id -RequiredResourceAccess @($clientReqAccess)

# Ensure client SP exists
$clientSp = Get-MgServicePrincipal -Filter "appId eq '$clientClientId'" -Top 1
if (-not $clientSp) { $clientSp = New-MgServicePrincipal -AppId $clientClientId }

# --- 3. Emit result --------------------------------------------------------
$out = [ordered]@{
    tenantId       = $TenantId
    apiClientId    = $apiClientId
    apiObjectId    = $apiApp.Id
    clientClientId = $clientClientId
    clientObjectId = $clientApp.Id
    apiScope       = "$identifierUri/access_as_user"
    appRoles       = $appRoles.Value
    nextSteps      = @(
        "1. In Entra portal, grant admin consent for both apps (API permissions blade).",
        "2. Assign the ARA-* security groups to the corresponding App Roles under Enterprise Applications → $apiName → Users and groups.",
        "3. Paste apiClientId and clientClientId into parameters.<env>.json before running the deploy pipeline."
    )
}
$out | ConvertTo-Json -Depth 5 | Out-File -FilePath $OutputFile -Encoding utf8
Write-Host "`n✓ Done. Output written to $OutputFile" -ForegroundColor Green
$out | ConvertTo-Json -Depth 5
