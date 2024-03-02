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
var topicNames = map(topics, topic => topic.name)
var subscriptions = reduce(map(topics, topic => map(topic.subscriptions, sub => { name: sub, topic: topic.name })), [], (acc, item) => concat(acc,item))

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
}

resource serviceBusTopics 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = [for topic in topicNames:{
  name: topic
  parent: serviceBusNamespace
}]

resource serviceBusTopicSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = [for subscription in subscriptions:{
  name: subscription.name
  parent: serviceBusTopics[indexOf(topicNames, subscription.topic)]
}]

resource ServiceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  scope: serviceBusNamespace
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
  }
}

output serviceBusEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint
