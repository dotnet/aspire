@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource storage2 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('storage2${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'storage2'
  }
}

resource storage2_blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: storage2
}

resource foocontainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  name: 'foo-container'
  parent: storage2_blobs
}

output blobEndpoint string = storage2.properties.primaryEndpoints.blob

output queueEndpoint string = storage2.properties.primaryEndpoints.queue

output tableEndpoint string = storage2.properties.primaryEndpoints.table

output name string = storage2.name