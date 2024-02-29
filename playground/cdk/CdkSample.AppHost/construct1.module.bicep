targetScope = 'resourceGroup'

@description('')
param storagesku string


resource storageAccount_SOTvKjFQy 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('bob', uniqueString(resourceGroup().id)), 24))
  location: resourceGroup().location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_SOTvKjFQy.properties.primaryEndpoints.table
