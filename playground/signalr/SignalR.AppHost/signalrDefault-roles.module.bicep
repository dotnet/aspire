@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param signalrdefault_outputs_name string

param principalType string

param principalId string

resource signalrDefault 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: signalrdefault_outputs_name
}

resource signalrDefault_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalrDefault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
    principalType: principalType
  }
  scope: signalrDefault
}