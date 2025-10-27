@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_eventhubs_name string

param existing_eventhubs_rg string

resource test_eventhubs 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
  name: existing_eventhubs_name
  scope: resourceGroup(existing_eventhubs_rg)
}