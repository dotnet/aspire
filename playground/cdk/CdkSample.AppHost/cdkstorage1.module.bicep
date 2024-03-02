targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param storagesku string


resource storageAccount_EMiEZBbox 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('cdkstorage1', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: storagesku
  }
  kind: 'Storage'
  properties: {
  }
}

output tableUri string = storageAccount_EMiEZBbox.properties.primaryEndpoints.table
