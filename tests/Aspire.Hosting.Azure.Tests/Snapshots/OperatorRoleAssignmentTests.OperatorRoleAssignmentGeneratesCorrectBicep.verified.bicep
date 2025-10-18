@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param operatorPrincipalId string

param storage_outputs_name string

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storage_outputs_name
}

resource storage_StorageAccountContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, operatorPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '17d1049b-9a84-46fb-8f53-869881c3d3ab'))
  properties: {
    principalId: operatorPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '17d1049b-9a84-46fb-8f53-869881c3d3ab')
    principalType: 'User'
  }
  scope: storage
}