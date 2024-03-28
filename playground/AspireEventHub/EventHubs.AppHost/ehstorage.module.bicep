targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource storageAccount_X1L2R0Ykm 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('ehstorage', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'ehstorage'
  }
  sku: {
    name: 'Standard_GRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

resource blobService_6Eo0U74qO 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount_X1L2R0Ykm
  name: 'default'
  properties: {
  }
}

resource roleAssignment_QJKNfwb9n 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_X1L2R0Ykm
  name: guid(storageAccount_X1L2R0Ykm.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_XX2YmbC7m 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_X1L2R0Ykm
  name: guid(storageAccount_X1L2R0Ykm.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_osDAIpL0k 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_X1L2R0Ykm
  name: guid(storageAccount_X1L2R0Ykm.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: principalId
    principalType: principalType
  }
}

output blobEndpoint string = storageAccount_X1L2R0Ykm.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_X1L2R0Ykm.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_X1L2R0Ykm.properties.primaryEndpoints.table
