targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource signalRService_iD3Yrl49T 'Microsoft.SignalRService/signalR@2022-02-01' = {
  name: toLower(take('signalr${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'signalr'
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

resource roleAssignment_35voRFfVj 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: signalRService_iD3Yrl49T
  name: guid(signalRService_iD3Yrl49T.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
    principalId: principalId
    principalType: principalType
  }
}

output hostName string = signalRService_iD3Yrl49T.properties.hostName
