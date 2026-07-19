// Log Analytics + Application Insights (workspace-based)
param logAnalyticsName string
param appInsightsName string
param location string
param tags object
param deployAppInsights bool = true

resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 90
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
  }
}

resource ai 'Microsoft.Insights/components@2020-02-02' = if (deployAppInsights) {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: law.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output logAnalyticsWorkspaceId    string = law.id
output appInsightsConnectionString string = ai.?properties.ConnectionString ?? ''
