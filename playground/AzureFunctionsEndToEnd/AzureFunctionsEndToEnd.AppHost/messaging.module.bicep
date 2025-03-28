@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('messaging-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'messaging'
  }
}

resource myqueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'myqueue'
  parent: messaging
}

output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint

output name string = messaging.name