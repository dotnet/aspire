targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Standard'

@description('')
param principalId string

@description('')
param principalType string


resource serviceBusNamespace_RuSlLOK64 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: toLower(take(concat('sb', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'sb'
  }
  sku: {
    name: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource roleAssignment_IS9HJzhT8 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBusNamespace_RuSlLOK64
  name: guid(serviceBusNamespace_RuSlLOK64.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalId: principalId
    principalType: principalType
  }
}

resource serviceBusQueue_XlB4dhrJO 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'queue1'
  location: location
  properties: {
  }
}

resource serviceBusTopic_bemnWZskJ 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'topic1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_pWgs2FLAX 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_bemnWZskJ
  name: 'subscription1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_qojP3oFII 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_bemnWZskJ
  name: 'subscription2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_Sh8X0ue6x 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'topic2'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_I0aPXc6VB 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic_Sh8X0ue6x
  name: 'subscription1'
  location: location
  properties: {
  }
}

output serviceBusEndpoint string = serviceBusNamespace_RuSlLOK64.properties.serviceBusEndpoint
