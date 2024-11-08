@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param storage_name string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storage_name
}

resource storage_ba92f5b4_2d11_453d_a403_e96b0029c9fe 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
  }
  scope: storage
}

output id string = identity.id

output clientId string = identity.properties.clientId