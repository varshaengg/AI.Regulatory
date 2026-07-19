// Azure AI Search — SystemAssigned identity for AAD-based data-source auth
param searchName string
param location string
param tags object
param sku string
param replicaCount int
param partitionCount int
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false

var srchName = toLower(searchName)

resource search 'Microsoft.Search/searchServices@2024-06-01-preview' = {
  name: srchName
  location: location
  tags: tags
  identity: { type: 'SystemAssigned' }
  sku: { name: sku }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: 'default'
    disableLocalAuth: false // TODO: flip to true once AAD wired end-to-end
    authOptions: {
      aadOrApiKey: { aadAuthFailureMode: 'http401WithBearerChallenge' }
    }
    semanticSearch: 'standard'
    publicNetworkAccess: 'enabled' // TODO: private endpoint post-MVP
  }
}

resource searchDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: search
  name: 'search-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [ { categoryGroup: 'allLogs', enabled: true } ]
    metrics: [ { category: 'AllMetrics', enabled: true } ]
  }
}

output searchName        string = search.name
output searchEndpoint    string = 'https://${search.name}.search.windows.net'
output searchPrincipalId string = search.identity.principalId
