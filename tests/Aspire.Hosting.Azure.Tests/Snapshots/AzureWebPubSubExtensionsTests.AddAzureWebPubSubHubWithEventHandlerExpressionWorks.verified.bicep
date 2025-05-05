﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Free_F1'

param capacity int = 1

param abc_url_0 string

resource wps1 'Microsoft.SignalRService/webPubSub@2024-03-01' = {
  name: take('wps1-${uniqueString(resourceGroup().id)}', 63)
  location: location
  sku: {
    name: sku
    capacity: capacity
  }
  tags: {
    'aspire-resource-name': 'wps1'
  }
}

resource abc 'Microsoft.SignalRService/webPubSub/hubs@2024-03-01' = {
  name: 'abc'
  properties: {
    eventHandlers: [
      {
        urlTemplate: abc_url_0
        userEventPattern: '*'
      }
    ]
  }
  parent: wps1
}

output endpoint string = 'https://${wps1.properties.hostName}'

output name string = wps1.name