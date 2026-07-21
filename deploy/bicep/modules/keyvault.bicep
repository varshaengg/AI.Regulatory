// Key Vault — RBAC-authorised, soft-delete + purge protection
param keyVaultName string
param location string
param tags object
param deployerObjectId string = ''
param workloadIdentityPrincipalId string = ''
param logAnalyticsWorkspaceId string = ''
param enableDiagnostics bool = false

// KV names must be <= 24 chars and globally unique. Truncate if the caller
// supplied a longer name (matches CAF convention: kv-<project>-<env>-<region>).
var kvName = length(keyVaultName) > 24 ? substring(keyVaultName, 0, 24) : keyVaultName

resource kv 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: kvName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled' // TODO: private endpoint post-MVP
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Grant the deployer Key Vault Administrator during deployment
resource kvAdminRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deployerObjectId)) {
  scope: kv
  name: guid(kv.id, deployerObjectId, 'kv-admin')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483') // Key Vault Administrator
    principalId: deployerObjectId
    principalType: 'ServicePrincipal'
  }
}

// Grant the workload UAMI 'Key Vault Secrets User' so app services can resolve
// @Microsoft.KeyVault(...) references via their user-assigned identity.
resource kvWorkloadRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(workloadIdentityPrincipalId)) {
  scope: kv
  name: guid(kv.id, workloadIdentityPrincipalId, 'kv-secrets-user')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: workloadIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource kvDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics && !empty(logAnalyticsWorkspaceId)) {
  scope: kv
  name: 'kv-diagnostics'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { categoryGroup: 'audit',   enabled: true }
      { categoryGroup: 'allLogs', enabled: true }
    ]
    metrics: [ { category: 'AllMetrics', enabled: true } ]
  }
}

output keyVaultName string = kv.name
output keyVaultUri  string = kv.properties.vaultUri
output keyVaultId   string = kv.id
