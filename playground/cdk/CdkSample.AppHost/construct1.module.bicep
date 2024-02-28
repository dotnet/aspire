targetScope = 'resourceGroup'

@description('')
param storageName string = 'bob${uniqueString(resourceGroup().id)}'

@description('West US 3')
param location string

@description('')
param storagesku string


resource storageAccount_zianFGpKu 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageName
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_zianFGpKu.properties.primaryEndpoints.table
