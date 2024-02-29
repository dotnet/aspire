param name string
param keyVaultName string

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
param sku string = 'free'

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

resource search 'Microsoft.Search/searchServices@2020-08-01' = {
  name: '${name}-${resourceToken}'
  location: location
  sku: {
    name: sku
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: hostingMode
  }
  tags: tags
}

var primaryKey = search.listAdminKeys(search.apiVersion).primaryKey
var searchServiceName = search.name

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
    name: keyVaultName

    resource secret 'secrets@2023-07-01' = {
        name: 'connectionString'
        properties: {
            value: 'Endpoint=https://${searchServiceName}.search.windows.net;Key=${primaryKey}'
        }
    }
}

