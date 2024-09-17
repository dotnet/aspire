@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

resource signalr 'Microsoft.SignalRService/signalR@2022-02-01' = {
    name: toLower(take('signalr${uniqueString(resourceGroup().id)}', 24))
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
        'aspire-resource-name': 'signalr'
    }
}

resource signalr_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(signalr.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
        principalType: principalType
    }
    scope: signalr
}

output hostName string = signalr.properties.hostName