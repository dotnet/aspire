@description('West US 3')
param location string

@description('')
param storagesku string


resource storageAccount_Jdw5JxFxB 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'bobb45549e63c694665b351f'
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

output tableUri string = storageAccount_Jdw5JxFxB.properties.primaryEndpoints.table
