@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource funcstorage67c6c 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('funcstorage67c6c${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  tags: {
    'aspire-resource-name': 'funcstorage67c6c'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: funcstorage67c6c
}

output blobEndpoint string = funcstorage67c6c.properties.primaryEndpoints.blob

output queueEndpoint string = funcstorage67c6c.properties.primaryEndpoints.queue

output tableEndpoint string = funcstorage67c6c.properties.primaryEndpoints.table

output name string = funcstorage67c6c.name