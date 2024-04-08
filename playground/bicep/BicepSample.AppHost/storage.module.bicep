targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource storageAccount_1XR3Um8QY 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take('storage${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'storage'
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

resource blobService_vTLU20GRg 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount_1XR3Um8QY
  name: 'default'
  properties: {
  }
}

resource roleAssignment_Gz09cEnxb 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_1XR3Um8QY
  name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_HRj6MDafS 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_1XR3Um8QY
  name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_r0wA6OpKE 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_1XR3Um8QY
  name: guid(storageAccount_1XR3Um8QY.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: principalId
    principalType: principalType
  }
}

output blobEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_1XR3Um8QY.properties.primaryEndpoints.table
