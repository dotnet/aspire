@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param myregistry_outputs_name string

param principalId string

resource myregistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: myregistry_outputs_name
}

resource myregistry_AcrPush 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(myregistry.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec')
    principalType: 'ServicePrincipal'
  }
  scope: myregistry
}