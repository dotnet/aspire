targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Standard'

@description('')
param principalId string

@description('')
param principalType string


resource eventHubsNamespace_wORIGuvCQ 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: toLower(take('eventhubns${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'eventhubns'
  }
  sku: {
    name: sku
  }
  properties: {
  }
}

resource roleAssignment_2so8CKuFt 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: eventHubsNamespace_wORIGuvCQ
  name: guid(eventHubsNamespace_wORIGuvCQ.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
    principalId: principalId
    principalType: principalType
  }
}

resource eventHub_4BpPMTltx 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  parent: eventHubsNamespace_wORIGuvCQ
  name: 'hub'
  location: location
  properties: {
  }
}

output eventHubsEndpoint string = eventHubsNamespace_wORIGuvCQ.properties.serviceBusEndpoint
