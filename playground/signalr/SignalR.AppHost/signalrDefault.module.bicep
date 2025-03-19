@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource signalrDefault 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: take('signalrDefault-${uniqueString(resourceGroup().id)}', 63)
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
    'aspire-resource-name': 'signalrDefault'
  }
}

resource signalrDefault_SignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalrDefault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
    principalType: principalType
  }
  scope: signalrDefault
}

output hostName string = signalrDefault.properties.hostName

output name string = signalrDefault.name