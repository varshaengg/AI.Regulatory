// =============================================================================
// App Service — single instance, feature-parameterized.
//   feature == 'web' : SPA / static content — Node runtime, no container image
//   feature != 'web' : API workload — Linux container from ACR (containerImage)
// Blue/green staging slot is created for every non-'web' feature.
// =============================================================================
param appServiceName string
param location string
param tags object
param planId string

@description('The suffix used when naming the app (e.g. web, api, sharedapi).')
param feature string

@description('Full container image reference (registry/repo:tag). Empty for stack-based deploys (e.g. Node).')
param containerImage string = ''

@description('Node version for stack-based web apps (used when containerImage is empty and feature==web).')
param nodeVersion string = '20-lts'

@description('.NET version for stack-based API apps (used when containerImage is empty and feature!=web).')
param dotnetVersion string = '8.0'

@description('Startup command for the SPA web app. Ignored for API stack deploys.')
param stackStartupCommand string = 'pm2 serve /home/site/wwwroot 8080 --no-daemon --spa'

@description('Key Vault URI passed via app settings (empty if KV not deployed).')
param keyVaultUri string = ''

@description('Key Vault secret reference for SQL connection string.')
param sqlConnectionKvRef string = ''

param openAiEndpoint string = ''
param searchEndpoint string = ''
param appInsightsConnString string = ''

@description('Extra runtime app-settings merged into siteConfig.appSettings (e.g. CORS origins, Entra IDs, feature flags). Sourced from deploy/config/appsettings.<feature>.<env>.json by the pipeline.')
param extraAppSettings array = []

@description('If true, creates a staging deployment slot (requires Standard+ SKU).')
param enableStagingSlot bool = true

@description('Resource id of a user-assigned managed identity to attach to the app (and its slot). Empty = system-assigned only.')
param userAssignedIdentityId string = ''

@description('Client (application) id of the user-assigned managed identity above. When set, the app gets AZURE_CLIENT_ID so DefaultAzureCredential picks the UAMI in dual-identity mode.')
param userAssignedIdentityClientId string = ''

var hasUami          = !empty(userAssignedIdentityId)
var identityBlock    = hasUami
  ? { type: 'SystemAssigned, UserAssigned', userAssignedIdentities: { '${userAssignedIdentityId}': {} } }
  : { type: 'SystemAssigned' }
var kvRefIdentity    = hasUami ? userAssignedIdentityId : 'SystemAssigned'
var uamiClientIdSettings = (hasUami && !empty(userAssignedIdentityClientId)) ? [
  { name: 'AZURE_CLIENT_ID', value: userAssignedIdentityClientId }
] : []

var isWeb        = feature == 'web'
var isContainer  = !empty(containerImage)
var linuxFxValue = isContainer
                   ? 'DOCKER|${containerImage}'
                   : (isWeb ? 'NODE|${nodeVersion}' : 'DOTNETCORE|${dotnetVersion}')

// Shared base app settings; the web app doesn't need SQL/OpenAI refs.
var baseAppSettings = [
  { name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE', value: 'false' }
  { name: 'WEBSITES_PORT',                       value: '8080' }
  { name: 'DOCKER_ENABLE_CI',                    value: 'false' }
]

var telemetrySettings = empty(appInsightsConnString) ? [] : [
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnString }
]

var apiOnlySettings = isWeb ? [] : concat(
  empty(keyVaultUri)        ? [] : [ { name: 'KeyVault__Uri',              value: keyVaultUri } ],
  empty(sqlConnectionKvRef) ? [] : [ { name: 'ConnectionStrings__ArtaSql', value: sqlConnectionKvRef } ],
  empty(openAiEndpoint)     ? [] : [ { name: 'AzureOpenAi__Endpoint',      value: openAiEndpoint } ],
  empty(searchEndpoint)     ? [] : [ { name: 'AzureAISearch__Endpoint',    value: searchEndpoint } ]
)

var webOnlySettings = !isWeb ? [] : [
  { name: 'API_BASE_URL',           value: '' }        // filled in post-deploy from api hostname
  { name: 'SCM_DO_BUILD_DURING_DEPLOYMENT', value: 'false' }
]

var appSettings = concat(baseAppSettings, telemetrySettings, uamiClientIdSettings, apiOnlySettings, webOnlySettings, extraAppSettings)

resource site 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  tags: tags
  kind: isContainer ? 'app,linux,container' : 'app,linux'
  identity: identityBlock
  properties: {
    serverFarmId: planId
    httpsOnly: true
    clientAffinityEnabled: false
    keyVaultReferenceIdentity: kvRefIdentity
    siteConfig: {
      linuxFxVersion: linuxFxValue
      appCommandLine: isContainer ? '' : (isWeb ? stackStartupCommand : '')
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      healthCheckPath: isWeb ? '/' : '/health/live'
      alwaysOn: true
      appSettings: appSettings
    }
  }
}

// Blue/green staging slot — only for backend services (skip static web).
resource staging 'Microsoft.Web/sites/slots@2024-04-01' = if (!isWeb && enableStagingSlot) {
  parent: site
  name: 'staging'
  location: location
  tags: tags
  kind: isContainer ? 'app,linux,container' : 'app,linux'
  identity: identityBlock
  properties: {
    serverFarmId: planId
    httpsOnly: true
    keyVaultReferenceIdentity: kvRefIdentity
    siteConfig: {
      linuxFxVersion: linuxFxValue
      appCommandLine: isContainer ? '' : ''
      minTlsVersion: '1.2'
      healthCheckPath: '/health/live'
      appSettings: appSettings
    }
  }
}

output appServiceName   string = site.name
output defaultHostName  string = 'https://${site.properties.defaultHostName}'
output principalId      string = site.identity.principalId
