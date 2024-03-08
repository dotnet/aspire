targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource serviceBusNamespace_RuSlLOK64 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: toLower(take(concat('sb', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: 'Standard'
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

resource serviceBusQueue_XlB4dhrJO 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'queue1'
  location: location
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 5
  }
}

resource serviceBusTopic_bemnWZskJ 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'topic1'
  location: location
  properties: {
    enablePartitioning: true
  }
}

resource serviceBusSubscription_pWgs2FLAX 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_bemnWZskJ
  name: 'subscription1'
  location: location
  properties: {
    lockDuration: 'PT5M'
    requiresSession: true
  }
}

resource serviceBusSubscription_qojP3oFII 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_bemnWZskJ
  name: 'subscription2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_Sh8X0ue6x 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'topic2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_Tian6H9Ne 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_RuSlLOK64
  name: 'topic3'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_zRiFtxcfV 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_Tian6H9Ne
  name: 'sub1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_tl6S45663 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_Tian6H9Ne
  name: 'sub2'
  location: location
  properties: {
  }
}

output serviceBusEndpoint string = serviceBusNamespace_RuSlLOK64.properties.serviceBusEndpoint
