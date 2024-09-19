@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

resource signalr1 'Microsoft.SignalRService/signalR@2022-02-01' = {
    name: toLower(take('signalr1${uniqueString(resourceGroup().id)}', 24))
    location: location
    properties: {
        cors: {
            allowedOrigins: [
                '*'
            ]
        }
        features: [
            {
                flag: 'ServiceMode'
                value: 'Default'
            }
        ]
    }
    kind: 'SignalR'
    sku: {
        name: 'Free_F1'
        capacity: 1
    }
    tags: {
        'aspire-resource-name': 'signalr1'
    }
}

resource signalr1_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(signalr1.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
        principalType: principalType
    }
    scope: signalr1
}

output hostName string = signalr1.properties.hostName