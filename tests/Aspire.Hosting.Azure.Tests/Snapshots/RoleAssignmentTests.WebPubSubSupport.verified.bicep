@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param webpubsub_outputs_name string

param principalId string

resource webpubsub 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
  name: webpubsub_outputs_name
}

resource webpubsub_WebPubSubServiceReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(webpubsub.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'bfb1c7d2-fb1a-466b-b2ba-aee63b92deaf'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'bfb1c7d2-fb1a-466b-b2ba-aee63b92deaf')
    principalType: 'ServicePrincipal'
  }
  scope: webpubsub
}