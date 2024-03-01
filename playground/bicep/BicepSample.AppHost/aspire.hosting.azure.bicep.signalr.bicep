param name string
param principalId string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The pricing tier of the SignalR resource.')
@allowed([
  'Free_F1'
  'Standard_S1'
  'Premium_P1'
])

param pricingTier string = 'Free_F1'

@description('The number of SignalR Unit.')
@allowed([
  1
  2
  5
  10
  20
  50
  100
])
param capacity int = 1

@description('Visit https://github.com/Azure/azure-signalr/blob/dev/docs/faq.md#service-mode to understand SignalR Service Mode.')
@allowed([
  'Default'
  'Serverless'
  'Classic'
])
param serviceMode string = 'Default'

@description('Set the list of origins that should be allowed to make cross-origin calls.')
param allowedOrigins array = [
  '*'
]

param principalType string = 'ServicePrincipal'

param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource signalR 'Microsoft.SignalRService/signalR@2022-02-01' = {
  name: replace('${name}-${resourceToken}', '-', '')
  location: location
  sku: {
    capacity: capacity
    name: pricingTier
  }
  kind: 'SignalR'
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: serviceMode
      }
    ]
    cors: {
      allowedOrigins: allowedOrigins
    }
  }
  tags: tags
}

// RoleName: SignalR App Server
var roleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '420fcaa2-552c-430f-98ca-3264be4806c7')
resource signalRRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalR.id, principalId, roleDefinitionId)
  scope: signalR
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: roleDefinitionId
  }
}

output hostName string = signalR.properties.hostName
