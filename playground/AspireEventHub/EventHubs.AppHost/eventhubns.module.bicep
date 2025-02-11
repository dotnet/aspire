@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

param principalType string

param principalId string

resource eventhubns 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('eventhubns-${uniqueString(resourceGroup().id)}', 256)
  location: location
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'eventhubns'
  }
}

resource eventhubns_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eventhubns.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
    principalType: principalType
  }
  scope: eventhubns
}

resource eventhub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'eventhub'
  parent: eventhubns
}

output eventHubsEndpoint string = eventhubns.properties.serviceBusEndpoint