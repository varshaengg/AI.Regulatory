// =============================================================================
// User-Assigned Managed Identity
// One workload identity shared across every app service + slot that talks to
// the data plane (SQL, KV, Storage, OpenAI, Search). Grant permissions once
// to this identity — every consumer that attaches it inherits the access.
// =============================================================================
param identityName string
param location string
param tags object

resource uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output name        string = uami.name
output resourceId  string = uami.id
output principalId string = uami.properties.principalId
output clientId    string = uami.properties.clientId
