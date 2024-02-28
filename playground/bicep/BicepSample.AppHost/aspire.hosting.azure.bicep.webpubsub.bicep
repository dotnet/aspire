param name string
param principalId string

@description('Tags that will be applied to all resources.')
param tags object = {}

@description('The pricing tier of the Web PubSub resource.')
@allowed([
  'Free_F1'
  'Standard_S1'
  'Premium_P1'
])

param pricingTier string = 'Free_F1'

@description('The unit count of the Web PubSub resource.')
@allowed([
  1
  2
  5
  10
  20
  50
  100
])
param capacity int = 1

param principalType string = 'ServicePrincipal'

param location string = resourceGroup().location
param hubSettings array = []

var resourceToken = uniqueString(resourceGroup().id)

resource webpubsub 'Microsoft.SignalRService/WebPubSub@2023-08-01-preview' = {
  name: replace('${name}-${resourceToken}', '-', '')
  location: location
  sku: {
    capacity: capacity
    name: pricingTier
  }
  tags: tags
  
  resource hubSetting 'hubs@2023-08-01-preview' = [for hub in hubSettings:{
    name: hub
    properties: {
      eventHandlers: [
        {
          urlTemplate: 'tunnel:///eventhandler'
          userEventPattern: '*'
          systemEvents: []
        }
      ]
      eventListeners: []
      anonymousConnectPolicy: 'deny'
    }
  }]
}

// RoleName: Web PubSub Service Owner: Full access to Azure Web PubSub Service REST APIs
var roleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
resource signalRRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(webpubsub.id, principalId, roleDefinitionId)
  scope: webpubsub
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: roleDefinitionId
  }
}


output endpoint string = 'https://${webpubsub.properties.hostName}'
