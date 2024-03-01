targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param storagesku string


resource storageAccount_WFnvkltok 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('bob', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_WFnvkltok.properties.primaryEndpoints.table
