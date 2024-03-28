targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Standard'

@description('')
param principalId string

@description('')
param principalType string


resource eventHubsNamespace_skb4aVCrD 'Microsoft.EventHub/namespaces@2022-10-01-preview' = {
  name: toLower(take(concat('eventhubns', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'eventhubns'
  }
  sku: {
    name: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource roleAssignment_cky0ZiKdq 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: eventHubsNamespace_skb4aVCrD
  name: guid(eventHubsNamespace_skb4aVCrD.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
    principalId: principalId
    principalType: principalType
  }
}

resource eventHub_BTiIwkSy2 'Microsoft.EventHub/namespaces/eventhubs@2022-10-01-preview' = {
  parent: eventHubsNamespace_skb4aVCrD
  name: 'hub'
  location: location
  properties: {
  }
}

output eventHubsEndpoint string = eventHubsNamespace_skb4aVCrD.properties.serviceBusEndpoint
