targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param storagesku string


resource storageAccount_unUi1Obb4 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('bob', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_unUi1Obb4.properties.primaryEndpoints.table
