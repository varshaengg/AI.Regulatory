// Azure OpenAI — gpt-4o-mini + text-embedding-3-large deployments
param openAiName string
param location string
param tags object
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false

@description('Deployment capacity for gpt-4o-mini in kTPM')
param gpt4oMiniCapacity int = 30

@description('Deployment capacity for text-embedding-3-large in kTPM')
param embeddingCapacity int = 60

var accountName = toLower(openAiName)

resource account 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'OpenAI'
  identity: { type: 'SystemAssigned' }
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled' // TODO: private endpoint post-MVP
    disableLocalAuth: false        // TODO: flip to true once all clients use AAD tokens
    networkAcls: {
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

resource depMini 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'gpt-4o-mini'
  sku: { name: 'Standard', capacity: gpt4oMiniCapacity }
  properties: {
    model: { format: 'OpenAI', name: 'gpt-4o-mini', version: '2024-07-18' }
    versionUpgradeOption: 'OnceCurrentVersionExpired'
  }
}

resource depEmbed 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'text-embedding-3-large'
  sku: { name: 'Standard', capacity: embeddingCapacity }
  properties: {
    model: { format: 'OpenAI', name: 'text-embedding-3-large', version: '1' }
    versionUpgradeOption: 'OnceCurrentVersionExpired'
  }
  dependsOn: [ depMini ] // serialize to avoid quota contention
}

resource aoaiDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: account
  name: 'aoai-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [ { categoryGroup: 'audit', enabled: true }, { categoryGroup: 'allLogs', enabled: true } ]
    metrics: [ { category: 'AllMetrics', enabled: true } ]
  }
}

output openAiName        string = account.name
output openAiEndpoint    string = account.properties.endpoint
output openAiPrincipalId string = account.identity.principalId
