targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Standard'

@description('')
param principalId string

@description('')
param principalType string


resource serviceBusNamespace_amM9gJ0Ya 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: toLower(take(concat('servicebus', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'servicebus'
  }
  sku: {
    name: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource roleAssignment_O68yhHszw 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBusNamespace_amM9gJ0Ya
  name: guid(serviceBusNamespace_amM9gJ0Ya.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalId: principalId
    principalType: principalType
  }
}

resource serviceBusQueue_wJ6B0eQwN 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace_amM9gJ0Ya
  name: 'queue1'
  location: location
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 5
  }
}

resource serviceBusTopic_Rr0YFQpE9 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace_amM9gJ0Ya
  name: 'topic1'
  location: location
  properties: {
    enablePartitioning: true
  }
}

resource serviceBusSubscription_SysEikGPG 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_Rr0YFQpE9
  name: 'subscription1'
  location: location
  properties: {
    lockDuration: 'PT5M'
    requiresSession: true
  }
}

resource serviceBusSubscription_5hExkZHCU 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_Rr0YFQpE9
  name: 'subscription2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_cKuAI6Z4Z 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace_amM9gJ0Ya
  name: 'topic2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_cRWE7uNBs 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace_amM9gJ0Ya
  name: 'topic3'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_bhjaa0Rpf 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_cRWE7uNBs
  name: 'sub1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_l4R4UcHly 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_cRWE7uNBs
  name: 'sub2'
  location: location
  properties: {
  }
}

output serviceBusEndpoint string = serviceBusNamespace_amM9gJ0Ya.properties.serviceBusEndpoint
