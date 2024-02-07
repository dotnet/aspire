param serviceBusNamespaceName string
param principalId string

@description('Tags that will be applied to all resources')
param tags object = {}

param principalType string = 'ServicePrincipal'
param sku string = 'Standard'

param location string = resourceGroup().location
param queues array = []
param topics array = []

var resourceToken = uniqueString(resourceGroup().id)

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${serviceBusNamespaceName}-${resourceToken}'
  location: location
  sku: {
    name: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
  tags: tags

  resource queue 'queues@2022-10-01-preview' = [for name in queues:{
    name: name
  }]

  resource topic 'topics@2022-10-01-preview' = [for name in topics:{
    name: name
  }]
}

resource ServiceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  scope: serviceBusNamespace
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
  }
}

output serviceBusEndpoint string = replace(serviceBusNamespace.properties.serviceBusEndpoint, 'https://', '')
