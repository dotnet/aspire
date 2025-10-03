@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource azure_storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('azurestorage${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: false
    isHnsEnabled: true
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  tags: {
    'aspire-resource-name': 'azure-storage'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: azure_storage
}

resource data_lake_file_system 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  name: 'data-lake-file-system'
  parent: blobs
}

output blobEndpoint string = azure_storage.properties.primaryEndpoints.blob

output dataLakeEndpoint string = azure_storage.properties.primaryEndpoints.dfs

output queueEndpoint string = azure_storage.properties.primaryEndpoints.queue

output tableEndpoint string = azure_storage.properties.primaryEndpoints.table

output name string = azure_storage.name