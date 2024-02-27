@description('')
param storagesku string

@description('')
param location string = 'West US 3'


resource storageAccount_9e2cRkGOF 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'bob99e053564f774257ad1d5'
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_9e2cRkGOF.properties.primaryEndpoints.table
