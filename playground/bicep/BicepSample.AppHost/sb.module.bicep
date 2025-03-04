@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

param principalType string

param principalId string

resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('sb-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'sb'
  }
}

resource sb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalType: principalType
  }
  scope: sb
}

resource queue1 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'queue1'
  parent: sb
}

resource topic1 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  name: 'topic1'
  parent: sb
}

resource subscription1 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  name: 'subscription1'
  parent: topic1
}

resource subscription2 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  name: 'subscription2'
  parent: topic1
}

resource topic2 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  name: 'topic2'
  parent: sb
}

resource topic2sub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  name: 'subscription1'
  parent: topic2
}

output serviceBusEndpoint string = sb.properties.serviceBusEndpoint