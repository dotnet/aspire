param principalId string

param principalType string

resource signalr 'Microsoft.SignalRService/signalR@2022-02-01' = {
    name: take('signalr-${uniqueString(resourceGroup().id)}', 63)
    location: resourceGroup().location
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

resource SignalRAppServer_signalr 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(resourceGroup().id, 'SignalRAppServer_signalr')
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
        principalType: principalType
    }
    scope: signalr
}

output hostName string = signalr.properties.hostName