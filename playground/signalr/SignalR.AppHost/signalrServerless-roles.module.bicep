@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param signalrserverless_outputs_name string

param principalType string

param principalId string

resource signalrServerless 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: signalrserverless_outputs_name
}

resource signalrServerless_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalrServerless.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
    principalType: principalType
  }
  scope: signalrServerless
}

resource signalrServerless_SignalRRestApiOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalrServerless.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'fd53cd77-2268-407a-8f46-7e7863d0f521'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'fd53cd77-2268-407a-8f46-7e7863d0f521')
    principalType: principalType
  }
  scope: signalrServerless
}