@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

resource eventHubs 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
  name: existingResourceName
}

output eventHubsEndpoint string = eventHubs.properties.serviceBusEndpoint

output name string = existingResourceName