// Shared Linux App Service Plan — used by all app services (web, api, ...)
param planName string
param location string
param tags object
param sku string
param instanceCount int

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: sku
    capacity: instanceCount
  }
  kind: 'linux'
  properties: {
    reserved: true
    zoneRedundant: contains([ 'P1v3', 'P2v3', 'P3v3' ], sku) && instanceCount >= 3
  }
}

output planId   string = plan.id
output planName string = plan.name
