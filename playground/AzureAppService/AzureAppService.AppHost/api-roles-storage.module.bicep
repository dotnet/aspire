@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param storage_outputs_name string

param principalId string

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storage_outputs_name
}

resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
  }
  scope: storage
}