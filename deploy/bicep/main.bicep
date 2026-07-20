// =============================================================================
// AI Regulatory (ARA) — Main entry (subscription scope)
// Provisions the resource group and delegates all resources to resources.bicep.
//
// Naming convention (Microsoft CAF short abbreviations):
//   RG:         rg-<project>-<env>-<regionShort>
//   Resource:   <kindShort>-<project>-<env>-<regionShort>[-<feature>]
// Example:      rg-RA-dev-wus, app-RA-dev-wus-web, app-RA-dev-wus-api, sql-RA-dev-wus
//
// Deploy:
//   az deployment sub create \
//       --location westus \
//       --template-file main.bicep \
//       --parameters @parameters.example.json
// =============================================================================
targetScope = 'subscription'

// -------------------------------------------------------------------------
// Core naming inputs
// -------------------------------------------------------------------------

@description('Environment short name — dev | test | uat | prod')
@allowed([ 'dev', 'test', 'uat', 'prod' ])
param environmentName string

@description('Project / product short code (2-6 letters, uppercase). Used in every resource name — e.g. RA.')
@minLength(2)
@maxLength(6)
param project string

@description('Azure region (e.g. westus, westeurope, swedencentral). Mapped to a short code in the resource names.')
param location string

@description('Sizing tier — small | medium | enterprise')
@allowed([ 'dev', 'small', 'medium', 'enterprise' ])
param sizingTier string = 'medium'

// -------------------------------------------------------------------------
// Resource catalog — the list of resources this deployment must provision.
// Each entry: { kind: '<kind>', feature: '<optional-suffix>' }
//   kind must be one of the keys in `kindShort` below.
//   feature is optional; omit for singleton resources (e.g. sql, kv).
// The pipeline / parameter file drives what actually gets deployed.
// -------------------------------------------------------------------------
@description('List of resources to deploy. Order does not matter; dependencies are enforced inside resources.bicep.')
param resources array = [
  { kind: 'logAnalytics' }
  { kind: 'appInsights' }
  { kind: 'keyVault' }
  { kind: 'storage' }
  { kind: 'sqlServer' }
  { kind: 'search' }
  { kind: 'openAi' }
  { kind: 'appServicePlan' }
  { kind: 'appService', feature: 'web' }
  { kind: 'appService', feature: 'api' }
]

// -------------------------------------------------------------------------
// Application / identity parameters (unchanged responsibilities)
// -------------------------------------------------------------------------

@description('Publisher container registry (source of API image)')
param publisherAcrLoginServer string = ''

@description('Container image tag deployed to the API app service (semver)')
param imageTag string = 'latest'

@description('Object ID of the deployer / service principal — receives KV Admin during deploy')
param deployerObjectId string = ''

@description('SQL admin AAD group object ID (used for AAD-only SQL auth)')
param sqlAadAdminGroupObjectId string = ''

@description('SQL admin AAD group display name')
param sqlAadAdminGroupName string = ''

@description('Map of feature -> array of extra appSettings entries. Pipeline builds this from deploy/config/appsettings.<feature>.<env>.json.')
param extraAppSettingsMap object = {}

// NOTE: `raAdminsGroupObjectId` (ARA Admin RBAC group) can be plumbed here once
// role assignments to ARA app services are added to resources.bicep. Kept out
// of the schema for now to avoid unused-param noise.

// -------------------------------------------------------------------------
// Region short-code map (Microsoft standard geo abbreviations)
// Fallback: any unknown region uses the first 3-4 chars of `location`.
// -------------------------------------------------------------------------
var regionShortMap = {
  westus:              'wus'
  westus2:             'wus2'
  westus3:             'wus3'
  eastus:              'eus'
  eastus2:             'eus2'
  centralus:           'cus'
  northcentralus:      'ncus'
  southcentralus:      'scus'
  westcentralus:       'wcus'
  canadacentral:       'canc'
  canadaeast:          'cane'
  westeurope:          'weu'
  northeurope:         'neu'
  uksouth:             'uks'
  ukwest:              'ukw'
  swedencentral:       'sdc'
  norwayeast:          'noe'
  francecentral:       'frc'
  switzerlandnorth:    'chn'
  germanywestcentral:  'gwc'
  polandcentral:       'plc'
  eastasia:            'ea'
  southeastasia:       'sea'
  japaneast:           'jpe'
  japanwest:           'jpw'
  koreacentral:        'krc'
  australiaeast:       'aue'
  australiasoutheast:  'auss'
  centralindia:        'cin'
  southindia:          'sin'
  brazilsouth:         'brs'
  uaenorth:            'uaen'
  southafricanorth:    'san'
}
var regionKey  = toLower(replace(location, ' ', ''))
var regionCode = regionShortMap[?regionKey] ?? substring(regionKey, 0, min(4, length(regionKey)))
// -------------------------------------------------------------------------
// Composed names
// -------------------------------------------------------------------------
var projectLower = toLower(project)
var namePrefix   = '${projectLower}-${environmentName}-${regionCode}'
var rgName       = 'rg-${namePrefix}'

var tags = {
  application: 'AI Regulatory Assistant'
  project:     project
  environment: environmentName
  region:      regionCode
  managedBy:   'ARA deploy pipeline'
  costCenter:  'regulatory'
}

// -------------------------------------------------------------------------
// Resource group
// -------------------------------------------------------------------------
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name:     rgName
  location: location
  tags:     tags
}

// -------------------------------------------------------------------------
// Delegate everything else to resources.bicep (RG scope)
// -------------------------------------------------------------------------
module workload 'resources.bicep' = {
  name:  'ara-workload-${environmentName}'
  scope: rg
  params: {
    location:                   location
    namePrefix:                 namePrefix
    tags:                       tags
    resources:                  resources
    sizingTier:                 sizingTier
    publisherAcrLoginServer:    publisherAcrLoginServer
    imageTag:                   imageTag
    deployerObjectId:           deployerObjectId
    sqlAadAdminGroupObjectId:   sqlAadAdminGroupObjectId
    sqlAadAdminGroupName:       sqlAadAdminGroupName
    extraAppSettingsMap:        extraAppSettingsMap
  }
}

output resourceGroupName string = rg.name
output regionCode        string = regionCode
output namePrefix        string = namePrefix
output apiHostName       string = workload.outputs.apiHostName
output webHostName       string = workload.outputs.webHostName
