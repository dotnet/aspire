@description('West US 3')
param location string

@description('')
param storagesku string


resource storageAccount_jUWh3LB5G 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'bob814ae87fd35d4c2988917'
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_jUWh3LB5G.properties.primaryEndpoints.table
