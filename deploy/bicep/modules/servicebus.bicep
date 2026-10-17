// Azure Service Bus — dossier-run work queue (API enqueues, worker consumes).
// AAD-only: local (SAS) auth is disabled and the workload identity gets data-plane roles.
param serviceBusName string
param location string
param tags object
param sku string = 'Standard'
param workloadIdentityPrincipalId string = ''
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false

@description('Queue that carries dossier-run stage commands.')
param dossierQueueName string = 'dossier-runs'

@description('Max delivery attempts before a message is dead-lettered.')
param maxDeliveryCount int = 5

var nsName = toLower(serviceBusName)

resource ns 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: nsName
  location: location
  tags: tags
  sku: { name: sku, tier: sku }
  properties: {
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled' // TODO: private endpoint post-MVP
  }
}

resource dossierQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: ns
  name: dossierQueueName
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: maxDeliveryCount
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P1D'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT1H'
  }
}

// Azure Service Bus Data Owner — API sends, worker receives (split into Sender/Receiver post-MVP).
resource workloadRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(workloadIdentityPrincipalId)) {
  scope: ns
  name: guid(ns.id, workloadIdentityPrincipalId, '090c5cfd-751d-490a-894a-3ce6f1109419')
  properties: {
    principalId: workloadIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
  }
}

resource sbDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: ns
  name: 'sb-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [ { categoryGroup: 'allLogs', enabled: true } ]
    metrics: [ { category: 'AllMetrics', enabled: true } ]
  }
}

output serviceBusName      string = ns.name
output serviceBusNamespace string = '${ns.name}.servicebus.windows.net'
output dossierQueueName    string = dossierQueue.name
