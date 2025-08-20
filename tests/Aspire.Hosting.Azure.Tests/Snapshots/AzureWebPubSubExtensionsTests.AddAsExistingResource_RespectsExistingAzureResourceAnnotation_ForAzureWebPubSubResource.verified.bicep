@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_webpubsub_name string

param existing_webpubsub_rg string

resource test_webpubsub 'Microsoft.SignalRService/webPubSub@2024-03-01' existing = {
  name: existing_webpubsub_name
  scope: resourceGroup(existing_webpubsub_rg)
}