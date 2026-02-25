@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource sql_stor 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('sqlstor${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: true
    isHnsEnabled: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Deny'
    }
    publicNetworkAccess: 'Disabled'
  }
  tags: {
    'aspire-resource-name': 'sql-stor'
  }
}

output blobEndpoint string = sql_stor.properties.primaryEndpoints.blob

output dataLakeEndpoint string = sql_stor.properties.primaryEndpoints.dfs

output queueEndpoint string = sql_stor.properties.primaryEndpoints.queue

output tableEndpoint string = sql_stor.properties.primaryEndpoints.table

output name string = sql_stor.name

output id string = sql_stor.id