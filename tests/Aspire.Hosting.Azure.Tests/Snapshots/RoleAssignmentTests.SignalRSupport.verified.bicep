@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param signalr_outputs_name string

param principalId string

resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: signalr_outputs_name
}

resource signalr_SignalRContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761')
    principalType: 'ServicePrincipal'
  }
  scope: signalr
}