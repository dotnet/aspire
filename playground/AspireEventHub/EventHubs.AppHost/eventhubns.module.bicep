@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

param principalId string

param principalType string

resource eventhubns 'Microsoft.EventHub/namespaces@2017-04-01' = {
    name: toLower(take('eventhubns${uniqueString(resourceGroup().id)}', 24))
    location: location
    sku: {
        name: sku
    }
    tags: {
        'aspire-resource-name': 'eventhubns'
    }
}

resource eventhubns_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(eventhubns.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
        principalType: principalType
    }
    scope: eventhubns
}

resource hub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
    name: 'hub'
    parent: eventhubns
}

output eventHubsEndpoint string = eventhubns.properties.serviceBusEndpoint