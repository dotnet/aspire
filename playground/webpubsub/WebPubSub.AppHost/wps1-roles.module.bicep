@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param wps1_outputs_name string

param principalType string

param principalId string

resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
  name: wps1_outputs_name
}

resource wps1_WebPubSubServiceOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(wps1.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
    principalType: principalType
  }
  scope: wps1
}