targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource signalRService_w7kmeqf4Y 'Microsoft.SignalRService/signalR@2022-02-01' = {
  name: toLower(take('signalr1${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'signalr1'
  }
  sku: {
    name: 'Free_F1'
    capacity: 1
  }
  kind: 'SignalR'
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
    ]
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
  }
}

resource roleAssignment_UoLS3XHY0 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: signalRService_w7kmeqf4Y
  name: guid(signalRService_w7kmeqf4Y.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
    principalId: principalId
    principalType: principalType
  }
}

output hostName string = signalRService_w7kmeqf4Y.properties.hostName
