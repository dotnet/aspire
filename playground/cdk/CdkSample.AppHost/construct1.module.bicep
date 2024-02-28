targetScope = 'resourceGroup'

@description('West US 3')
param location string

@description('')
param storagesku string


resource storageAccount_WLTg4zEgJ 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'bobd3c49aedcdfc40eb8f3d8'
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_WLTg4zEgJ.properties.primaryEndpoints.table
