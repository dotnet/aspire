targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource storageAccount_59pNEh4K4 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take('ehstorage${uniqueString(resourceGroup().id)}', 24))
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
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

resource blobService_5WQ0wIU09 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount_59pNEh4K4
  name: 'default'
  properties: {
  }
}

resource roleAssignment_LrhalXYOv 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_59pNEh4K4
  name: guid(storageAccount_59pNEh4K4.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_DDmC2vNE8 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_59pNEh4K4
  name: guid(storageAccount_59pNEh4K4.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_4tZWfnMZF 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_59pNEh4K4
  name: guid(storageAccount_59pNEh4K4.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: principalId
    principalType: principalType
  }
}

output blobEndpoint string = storageAccount_59pNEh4K4.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_59pNEh4K4.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_59pNEh4K4.properties.primaryEndpoints.table
