@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('storage${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'storage'
  }
}

resource queues 'Microsoft.Storage/storageAccounts/queueServices@2024-01-01' = {
  name: 'default'
  parent: storage
}

resource queue1 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-01-01' = {
  name: 'queue1'
  parent: queues
}

resource queue2 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-01-01' = {
  name: 'queue2'
  parent: queues
}

output blobEndpoint string = storage.properties.primaryEndpoints.blob

output queueEndpoint string = storage.properties.primaryEndpoints.queue

output tableEndpoint string = storage.properties.primaryEndpoints.table

output name string = storage.name
