@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_signalr_name string

resource test_signalr 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: existing_signalr_name
}

output hostName string = test_signalr.properties.hostName

output name string = existing_signalr_name