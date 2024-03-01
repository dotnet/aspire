targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param storagesku string

@description('')
param principalId string

@description('')
param principalType string


resource storageAccount_RmpSJ3Cvw 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('cdkstorage', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
  }
}

resource roleAssignment_fgqOZM0lW 'Microsoft.Resources/roleAssignments@2022-04-01' = {
  scope: storageAccount_RmpSJ3Cvw
  name: guid('storageAccount_RmpSJ3Cvw', principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_fgqOZM0lW 'Microsoft.Resources/roleAssignments@2022-04-01' = {
  scope: storageAccount_RmpSJ3Cvw
  name: guid('storageAccount_RmpSJ3Cvw', principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_fgqOZM0lW 'Microsoft.Resources/roleAssignments@2022-04-01' = {
  scope: storageAccount_RmpSJ3Cvw
  name: guid('storageAccount_RmpSJ3Cvw', principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: principalId
    principalType: principalType
  }
}

output blobEndpoint string = storageAccount_RmpSJ3Cvw.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_RmpSJ3Cvw.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_RmpSJ3Cvw.properties.primaryEndpoints.table
