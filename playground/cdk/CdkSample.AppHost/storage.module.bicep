targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param storagesku string

@description('')
param locationOverride string


resource storageAccount_65zdmu5tK 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('storage', uniqueString(resourceGroup().id)), 24))
  location: locationOverride
  tags: {
    'aspire-resource-name': 'storage'
  }
  sku: {
    name: storagesku
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

resource blobService_24WqMwYy8 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount_65zdmu5tK
  name: 'default'
  properties: {
  }
}

resource roleAssignment_ryHNwVXTs 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_65zdmu5tK
  name: guid(storageAccount_65zdmu5tK.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_hqRD0luQx 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_65zdmu5tK
  name: guid(storageAccount_65zdmu5tK.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_5PGf5zmoW 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_65zdmu5tK
  name: guid(storageAccount_65zdmu5tK.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: principalId
    principalType: principalType
  }
}

output blobEndpoint string = storageAccount_65zdmu5tK.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_65zdmu5tK.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_65zdmu5tK.properties.primaryEndpoints.table
