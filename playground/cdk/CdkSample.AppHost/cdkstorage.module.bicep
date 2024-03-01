targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param storagesku string


resource storageAccount_RmpSJ3Cvw 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('cdkstorage', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output blobEndpoint string = storageAccount_RmpSJ3Cvw.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_RmpSJ3Cvw.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_RmpSJ3Cvw.properties.primaryEndpoints.table
