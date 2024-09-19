@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Free_F1'

param capacity int = 1

param principalId string

param principalType string

resource wps1 'Microsoft.SignalRService/webPubSub@2021-10-01' = {
    name: toLower(take('wps1${uniqueString(resourceGroup().id)}', 24))
    location: location
    sku: {
        name: sku
        capacity: capacity
    }
    tags: {
        'aspire-resource-name': 'wps1'
    }
}

resource WebPubSubServiceOwner_wps1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(resourceGroup().id, 'WebPubSubServiceOwner_wps1')
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '12cf5a90-567b-43ae-8102-96cf46c7d9b4')
        principalType: principalType
    }
    scope: wps1
}

output endpoint string = 'https://${wps1.properties.hostName}'