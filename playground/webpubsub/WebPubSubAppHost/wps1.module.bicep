targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Free_F1'

@description('')
param capacity int = 1

@description('')
param principalId string

@description('')
param principalType string


resource webPubSubService_L5mmKvg0U 'Microsoft.SignalRService/webPubSub@2021-10-01' = {
  name: toLower(take('wps1${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'wps1'
  }
  sku: {
    name: sku
    capacity: capacity
  }
  properties: {
  }
}

resource roleAssignment_yvXMOMBDZ 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: webPubSubService_L5mmKvg0U
  name: guid(webPubSubService_L5mmKvg0U.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
    principalId: principalId
    principalType: principalType
  }
}

output endpoint string = 'https://${webPubSubService_L5mmKvg0U.properties.hostName}'
