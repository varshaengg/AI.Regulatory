// =============================================================================
// ARA — Resource-group scope orchestration
// Loops over the `resources` catalog and provisions each requested resource.
// Naming: <kindShort>-<project>-<env>-<regionShort>[-<feature>]  (all lower-case)
// =============================================================================
targetScope = 'resourceGroup'

param location string
param namePrefix string
param tags object
param resources array
param sizingTier string
param publisherAcrLoginServer string
param imageTag string
param deployerObjectId string
param sqlAadAdminGroupObjectId string
param sqlAadAdminGroupName string

@description('Map of feature -> array of extra appSettings entries ({name,value}). Fed by the pipeline from deploy/config/appsettings.<feature>.<env>.json.')
param extraAppSettingsMap object = {}

// -------------------------------------------------------------------------
// Sizing presets
// -------------------------------------------------------------------------
var sizing = {
  dev: {
    sqlSku:          { name: 'Basic', tier: 'Basic', capacity: 5 }
    searchSku:       'basic'
    searchReplicas:  1
    searchPartitions:1
    appServiceSku:   'B1'
    appServiceCount: 1
  }
  small: {
    sqlSku:          { name: 'GP_Gen5_2', tier: 'GeneralPurpose', capacity: 2 }
    searchSku:       'standard'
    searchReplicas:  1
    searchPartitions:1
    appServiceSku:   'P1v3'
    appServiceCount: 1
  }
  medium: {
    sqlSku:          { name: 'GP_Gen5_4', tier: 'GeneralPurpose', capacity: 4 }
    searchSku:       'standard2'
    searchReplicas:  2
    searchPartitions:2
    appServiceSku:   'P1v3'
    appServiceCount: 1
  }
  enterprise: {
    sqlSku:          { name: 'BC_Gen5_8', tier: 'BusinessCritical', capacity: 8 }
    searchSku:       'standard3'
    searchReplicas:  3
    searchPartitions:3
    appServiceSku:   'P2v3'
    appServiceCount: 2
  }
}
var s = sizing[sizingTier]

// -------------------------------------------------------------------------
// Resource-kind short code map (Microsoft CAF abbreviations)
// -------------------------------------------------------------------------
var kindShort = {
  appService:        'app'
  appServicePlan:    'plan'
  sqlServer:         'sql'
  sqlDatabase:       'sqldb'
  storage:           'st'
  keyVault:          'kv'
  logAnalytics:      'log'
  appInsights:       'appi'
  openAi:            'oai'
  search:            'srch'
  containerRegistry: 'cr'
}

// -------------------------------------------------------------------------
// Resource-catalog helpers
// -------------------------------------------------------------------------
func hasKind(items array, kind string) bool =>
  length(filter(items, r => r.kind == kind)) > 0

func pickKind(items array, kind string) array =>
  filter(items, r => r.kind == kind)

var appServiceEntries = pickKind(resources, 'appService')
var wantLogAnalytics  = hasKind(resources, 'logAnalytics')
var wantAppInsights   = hasKind(resources, 'appInsights')
var wantKeyVault      = hasKind(resources, 'keyVault')
var wantStorage       = hasKind(resources, 'storage')
var wantSql           = hasKind(resources, 'sqlServer')
var wantSearch        = hasKind(resources, 'search')
var wantOpenAi        = hasKind(resources, 'openAi')
var wantAppPlan       = hasKind(resources, 'appServicePlan') || length(appServiceEntries) > 0

// -------------------------------------------------------------------------
// Name helpers (single source of truth)
// -------------------------------------------------------------------------
func name(kindShortMap object, kind string, prefix string, feature string) string =>
  empty(feature) ? '${kindShortMap[kind]}-${prefix}' : '${kindShortMap[kind]}-${prefix}-${feature}'

// Concrete singleton names (feature omitted)
var logName    = name(kindShort, 'logAnalytics',   namePrefix, '')
var appiName   = name(kindShort, 'appInsights',    namePrefix, '')
var kvName     = name(kindShort, 'keyVault',       namePrefix, '')
var stName     = name(kindShort, 'storage',        namePrefix, '')
var sqlName    = name(kindShort, 'sqlServer',      namePrefix, '')
var srchName   = name(kindShort, 'search',         namePrefix, '')
var oaiName    = name(kindShort, 'openAi',         namePrefix, '')
var planName   = name(kindShort, 'appServicePlan', namePrefix, '')

// -------------------------------------------------------------------------
// Modules — deployed conditionally based on `resources` catalog
// -------------------------------------------------------------------------
module monitoring 'modules/monitoring.bicep' = if (wantLogAnalytics || wantAppInsights) {
  name: 'mod-monitoring'
  params: {
    logAnalyticsName: logName
    appInsightsName:  appiName
    location:         location
    tags:             tags
    deployAppInsights: wantAppInsights
  }
}

module keyvault 'modules/keyvault.bicep' = if (wantKeyVault) {
  name: 'mod-keyvault'
  params: {
    keyVaultName:             kvName
    location:                 location
    tags:                     tags
    deployerObjectId:         deployerObjectId
    logAnalyticsWorkspaceId:  monitoring.?outputs.logAnalyticsWorkspaceId ?? ''
    enableDiagnostics:        wantLogAnalytics
  }
}

module storage 'modules/storage.bicep' = if (wantStorage) {
  name: 'mod-storage'
  params: {
    storageBaseName:          stName
    location:                 location
    tags:                     tags
    logAnalyticsWorkspaceId:  monitoring.?outputs.logAnalyticsWorkspaceId ?? ''
    enableDiagnostics:        wantLogAnalytics
    skuName:                  sizingTier == 'dev' ? 'Standard_LRS' : 'Standard_GZRS'
  }
}

module sql 'modules/sql.bicep' = if (wantSql) {
  name: 'mod-sql'
  params: {
    sqlServerName:            sqlName
    location:                 location
    tags:                     tags
    sqlSku:                   s.sqlSku
    aadAdminGroupObjectId:    sqlAadAdminGroupObjectId
    aadAdminGroupName:        sqlAadAdminGroupName
    logAnalyticsWorkspaceId:  monitoring.?outputs.logAnalyticsWorkspaceId ?? ''
    enableDiagnostics:        wantLogAnalytics
  }
}

module search 'modules/search.bicep' = if (wantSearch) {
  name: 'mod-search'
  params: {
    searchName:               srchName
    location:                 location
    tags:                     tags
    sku:                      s.searchSku
    replicaCount:             s.searchReplicas
    partitionCount:           s.searchPartitions
    logAnalyticsWorkspaceId:  monitoring.?outputs.logAnalyticsWorkspaceId ?? ''
    enableDiagnostics:        wantLogAnalytics
  }
}

module openai 'modules/openai.bicep' = if (wantOpenAi) {
  name: 'mod-openai'
  params: {
    openAiName:               oaiName
    location:                 location
    tags:                     tags
    logAnalyticsWorkspaceId:  monitoring.?outputs.logAnalyticsWorkspaceId ?? ''
    enableDiagnostics:        wantLogAnalytics
  }
}

module plan 'modules/appserviceplan.bicep' = if (wantAppPlan) {
  name: 'mod-appserviceplan'
  params: {
    planName:      planName
    location:      location
    tags:          tags
    sku:           s.appServiceSku
    instanceCount: s.appServiceCount
  }
}

// -------------------------------------------------------------------------
// App services — loop the catalog. Each entry gets its own name via feature.
// The 'web' feature is treated as the static SPA (NODE stack), any other
// feature gets a Linux container (the API and future *api services).
// -------------------------------------------------------------------------
module appServices 'modules/appservice.bicep' = [for (svc, i) in appServiceEntries: {
  name: 'mod-app-${svc.feature}'
  params: {
    appServiceName:     '${kindShort.appService}-${namePrefix}-${svc.feature}'
    location:           location
    tags:               tags
    planId:             plan.?outputs.planId ?? ''
    feature:            svc.feature
    containerImage:     svc.feature == 'web'
                        ? ''
                        : (empty(publisherAcrLoginServer) ? '' : '${publisherAcrLoginServer}/ara-api:${imageTag}')
    keyVaultUri:           keyvault.?outputs.keyVaultUri ?? ''
    sqlConnectionKvRef:    wantKeyVault
                           ? '@Microsoft.KeyVault(VaultName=${keyvault.?outputs.keyVaultName ?? ''};SecretName=sql-connection-string)'
                           : ''
    openAiEndpoint:        openai.?outputs.openAiEndpoint ?? ''
    searchEndpoint:        search.?outputs.searchEndpoint ?? ''
    appInsightsConnString: monitoring.?outputs.appInsightsConnectionString ?? ''
    extraAppSettings:      contains(extraAppSettingsMap, svc.feature) ? extraAppSettingsMap[svc.feature] : []
    enableStagingSlot:     !startsWith(s.appServiceSku, 'B')
  }
}]

// -------------------------------------------------------------------------
// Convenience outputs — pick 'web' + 'api' host names if present
// -------------------------------------------------------------------------
var webIdx = indexOf(map(appServiceEntries, e => e.feature), 'web')
var apiIdx = indexOf(map(appServiceEntries, e => e.feature), 'api')

output apiHostName    string = apiIdx >= 0 ? appServices[apiIdx].outputs.defaultHostName : ''
output webHostName    string = webIdx >= 0 ? appServices[webIdx].outputs.defaultHostName : ''
output apiAppName     string = apiIdx >= 0 ? appServices[apiIdx].outputs.appServiceName  : ''
output webAppName     string = webIdx >= 0 ? appServices[webIdx].outputs.appServiceName  : ''
output sqlServerFqdn  string = sql.?outputs.serverFqdn        ?? ''
output keyVaultUri    string = keyvault.?outputs.keyVaultUri  ?? ''
output searchEndpoint string = search.?outputs.searchEndpoint ?? ''
output openAiEndpoint string = openai.?outputs.openAiEndpoint ?? ''
