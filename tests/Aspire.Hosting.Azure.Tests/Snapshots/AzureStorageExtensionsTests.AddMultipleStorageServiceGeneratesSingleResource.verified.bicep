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

resource blobService2 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: storage
}

resource container1 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  name: 'container1'
  parent: blobService2
}

resource container2 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  name: 'container2'
  parent: blobService2
}

resource queueService2 'Microsoft.Storage/storageAccounts/queueServices@2024-01-01' = {
  name: 'default'
  parent: storage
}

resource queue1 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-01-01' = {
  name: 'queue1'
  parent: queueService2
}

resource queue2 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-01-01' = {
  name: 'queue2'
  parent: queueService2
}

resource tableService2 'Microsoft.Storage/storageAccounts/tableServices@2024-01-01' = {
  name: 'default'
  parent: storage
}

output blobEndpoint string = storage.properties.primaryEndpoints.blob

output queueEndpoint string = storage.properties.primaryEndpoints.queue

output tableEndpoint string = storage.properties.primaryEndpoints.table

output name string = storage.name