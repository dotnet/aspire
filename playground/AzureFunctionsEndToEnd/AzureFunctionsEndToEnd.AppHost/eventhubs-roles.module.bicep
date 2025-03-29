@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param eventhubs_outputs_name string

param principalType string

param principalId string

resource eventhubs 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
  name: eventhubs_outputs_name
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