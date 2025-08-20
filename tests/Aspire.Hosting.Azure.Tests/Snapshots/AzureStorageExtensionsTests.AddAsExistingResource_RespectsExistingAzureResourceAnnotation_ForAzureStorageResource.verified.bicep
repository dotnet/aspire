@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_storage_name string

resource test_storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: existing_storage_name
}

output blobEndpoint string = test_storage.properties.primaryEndpoints.blob

output queueEndpoint string = test_storage.properties.primaryEndpoints.queue

output tableEndpoint string = test_storage.properties.primaryEndpoints.table

output name string = existing_storage_name