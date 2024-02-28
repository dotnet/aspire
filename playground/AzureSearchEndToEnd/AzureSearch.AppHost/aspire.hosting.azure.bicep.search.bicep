param name string
param principalId string
param principalType string = 'ServicePrincipal'

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The pricing tier of the search service you want to create (for example, basic or standard).')
@allowed([
  'free'
  'basic'
  'standard'
  'standard2'
  'standard3'
  'storage_optimized_l1'
  'storage_optimized_l2'
])
param sku string = 'basic'

@description('Replicas distribute search workloads across the service. You need at least two replicas to support high availability of query workloads (not applicable to the free tier).')
@minValue(1)
@maxValue(12)
param replicaCount int = 1

@description('Partitions allow for scaling of document count as well as faster indexing by sharding your index over multiple search units.')
@allowed([
  1
  2
  3
  4
  6
  12
])
param partitionCount int = 1

@description('Applicable only for SKUs set to standard3. You can set this property to enable a single, high density partition that allows up to 1000 indexes, which is much higher than the maximum indexes allowed for any other SKU.')
@allowed([
  'default'
  'highDensity'
])
param hostingMode string = 'default'

@description('Location for all resources.')
param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource search 'Microsoft.Search/searchServices@2022-09-01' = {
  name: '${name}-${resourceToken}'
  location: location
  sku: {
    name: sku
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: hostingMode
    disableLocalAuth: true
  }
  tags: tags
}

// Find list of roles and GUIDs in https://learn.microsoft.com/azure/role-based-access-control/built-in-roles

// Search Service Contributor
var searchServiceContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
resource searchServiceContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, principalId, searchServiceContributorRole)
  scope: search
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: searchServiceContributorRole
  }
}

// Search Index Data Contributor
var searchIndexDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
resource searchIndexDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, principalId, searchIndexDataContributorRole)
  scope: search
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: searchIndexDataContributorRole
  }
}

// The resource provider doesn't expose the final endpoint url, so we construct it from the unique name
output connectionString string = 'Endpoint=https://${search.name}.search.windows.net'
