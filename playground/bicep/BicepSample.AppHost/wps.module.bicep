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


resource webPubSubService_e7AgYwyOW 'Microsoft.SignalRService/webPubSub@2021-10-01' = {
  name: toLower(take('wps${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'wps'
  }
  sku: {
    name: sku
    capacity: capacity
  }
  properties: {
  }
}

resource roleAssignment_TqCIYDDW7 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: webPubSubService_e7AgYwyOW
  name: guid(webPubSubService_e7AgYwyOW.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
    principalId: principalId
    principalType: principalType
  }
}

output endpoint string = 'https://${webPubSubService_e7AgYwyOW.properties.hostName}'
