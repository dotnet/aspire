@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

param principalType string

param principalId string

resource sbemulator 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('sbemulator-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'sbemulator'
  }
}

resource sbemulator_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sbemulator.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalType: principalType
  }
  scope: sbemulator
}

resource queue1 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'queue1'
  properties: {
    deadLetteringOnMessageExpiration: false
  }
  parent: sbemulator
}

resource topic1 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  name: 'topic1'
  parent: sbemulator
}

resource sub1 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  name: 'sub1'
  properties: {
    maxDeliveryCount: 10
  }
  parent: topic1
}

resource app_prop_filter_1 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = {
  name: 'app-prop-filter-1'
  properties: {
    correlationFilter: {
      correlationId: 'id1'
      messageId: 'msgid1'
      to: 'xyz'
      replyTo: 'someQueue'
      label: 'subject1'
      sessionId: 'session1'
      replyToSessionId: 'sessionId'
      contentType: 'application/text'
    }
    filterType: 'CorrelationFilter'
  }
  parent: sub1
}

output serviceBusEndpoint string = sbemulator.properties.serviceBusEndpoint