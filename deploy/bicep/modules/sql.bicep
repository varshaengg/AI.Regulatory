// Azure SQL — AAD-only auth
param sqlServerName string
param location string
param tags object
param sqlSku object
param aadAdminGroupObjectId string = ''
param aadAdminGroupName string = ''
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false
param databaseName string = 'ara'

var srvName = toLower(sqlServerName)

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: srvName
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled' // TODO: private endpoint post-MVP
    administrators: empty(aadAdminGroupObjectId) ? null : {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: aadAdminGroupName
      sid: aadAdminGroupObjectId
      tenantId: subscription().tenantId
      principalType: 'Group'
    }
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: sqlSku
  properties: {
    zoneRedundant: contains([ 'BusinessCritical', 'Premium' ], sqlSku.tier)
    requestedBackupStorageRedundancy: 'Zone'
    readScale: 'Disabled'
  }
}

resource sqlFwAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress:   '0.0.0.0'
  }
}

resource sqlDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: sqlDb
  name: 'sql-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { category: 'SQLSecurityAuditEvents',       enabled: true }
      { category: 'SQLInsights',                  enabled: true }
      { category: 'AutomaticTuning',              enabled: true }
      { category: 'QueryStoreRuntimeStatistics',  enabled: true }
      { category: 'Errors',                       enabled: true }
    ]
    metrics: [ { category: 'Basic', enabled: true } ]
  }
}

output serverFqdn   string = sqlServer.properties.fullyQualifiedDomainName
output serverName   string = sqlServer.name
output databaseName string = sqlDb.name
