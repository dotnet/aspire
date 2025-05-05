@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

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

resource eventhubOne 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'eventhub'
  parent: eventhubns
}

output eventHubsEndpoint string = eventhubns.properties.serviceBusEndpoint

output name string = eventhubns.name