@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

param principalType string

param principalId string

resource eventhubs 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('eventhubs-${uniqueString(resourceGroup().id)}', 256)
  location: location
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'eventhubs'
  }
}

resource eventhubs_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eventhubs.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
    principalType: principalType
  }
  scope: eventhubs
}

resource myhub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'myhub'
  parent: eventhubs
}

output eventHubsEndpoint string = eventhubs.properties.serviceBusEndpoint