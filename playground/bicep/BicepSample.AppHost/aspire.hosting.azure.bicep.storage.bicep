param storageName string

param principalId string

param principalType string = 'ServicePrincipal'

param sku string = 'Standard_GRS'

param kind string = 'Storage'

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The location used for all deployed resources')
param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource storage 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: replace('${storageName}-${resourceToken}', '-', '')
  location: location
  kind: kind
  sku: {
    name: sku
  }
  tags: tags

  resource blobs 'blobServices@2022-05-01' = {
    name: 'default'
  }
}

resource BlobsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  scope: storage
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource QueuesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  scope: storage
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
  }
}

resource TablesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  scope: storage
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
  }
}

output blobEndpoint string = storage.properties.primaryEndpoints.blob
output queueEndpoint string = storage.properties.primaryEndpoints.queue
output tableEndpoint string = storage.properties.primaryEndpoints.table
