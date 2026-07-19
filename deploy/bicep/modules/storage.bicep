// Storage — ADLS Gen2. Storage account names must be 3-24 chars, lowercase, no dashes.
param storageBaseName string
param location string
param tags object
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false
@description('Storage redundancy SKU. Standard_LRS for dev, Standard_GZRS for prod (must exist in region).')
param skuName string = 'Standard_GZRS'

// Strip dashes + truncate to 24. Callers pass e.g. `st-ra-dev-wus` -> `stradevwus`.
var stgClean = toLower(replace(storageBaseName, '-', ''))
var stgName  = length(stgClean) > 24 ? substring(stgClean, 0, 24) : stgClean

resource stg 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: stgName
  location: location
  tags: tags
  sku: { name: skuName }
  kind: 'StorageV2'
  properties: {
    isHnsEnabled: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    publicNetworkAccess: 'Enabled' // TODO: private endpoint post-MVP
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: { enabled: true, keyType: 'Account' }
        file: { enabled: true, keyType: 'Account' }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobSvc 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: stg
  name: 'default'
  properties: {
    deleteRetentionPolicy: { enabled: true, days: 30 }
    containerDeleteRetentionPolicy: { enabled: true, days: 30 }
  }
}

resource cContent  'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { parent: blobSvc, name: 'content',           properties: { publicAccess: 'None' } }
resource cCatalog  'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { parent: blobSvc, name: 'ara-catalog',       properties: { publicAccess: 'None' } }
resource cOverride 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { parent: blobSvc, name: 'ara-overrides',     properties: { publicAccess: 'None' } }
resource cDossier  'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { parent: blobSvc, name: 'dossier-packages', properties: { publicAccess: 'None' } }
resource cAudit    'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { parent: blobSvc, name: 'audit-archive',     properties: { publicAccess: 'None' } }

resource stgDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: blobSvc
  name: 'stg-blob-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { category: 'StorageRead',   enabled: true }
      { category: 'StorageWrite',  enabled: true }
      { category: 'StorageDelete', enabled: true }
    ]
    metrics: [ { category: 'AllMetrics', enabled: true } ]
  }
}

output storageAccountName string = stg.name
output storageAccountId   string = stg.id
output blobEndpoint       string = stg.properties.primaryEndpoints.blob
