@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sb_outputs_name string

param principalId string

resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: sb_outputs_name
}

resource sb_AzureServiceBusDataReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
    principalType: 'ServicePrincipal'
  }
  scope: sb
}

resource sb_AzureServiceBusDataSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
    principalType: 'ServicePrincipal'
  }
  scope: sb
}