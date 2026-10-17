// Azure AI Document Intelligence — layout/OCR for template PDFs and source documents.
// Called with AAD tokens by the workload identity (Cognitive Services User).
param documentIntelligenceName string
param location string
param tags object
param workloadIdentityPrincipalId string = ''
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false

var accountName = toLower(documentIntelligenceName)

resource account 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'FormRecognizer'
  identity: { type: 'SystemAssigned' }
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled' // TODO: private endpoint post-MVP
    disableLocalAuth: true
    networkAcls: {
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

// Cognitive Services User
resource workloadRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(workloadIdentityPrincipalId)) {
  scope: account
  name: guid(account.id, workloadIdentityPrincipalId, 'a97b65f3-24c7-4388-baec-2e87135dc908')
  properties: {
    principalId: workloadIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
  }
}

resource diDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: account
  name: 'di-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [ { categoryGroup: 'audit', enabled: true }, { categoryGroup: 'allLogs', enabled: true } ]
    metrics: [ { category: 'AllMetrics', enabled: true } ]
  }
}

output documentIntelligenceName     string = account.name
output documentIntelligenceEndpoint string = account.properties.endpoint
