targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Standard'

@description('')
param principalId string

@description('')
param principalType string


resource serviceBusNamespace_eRbchjzJN 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: toLower(take('servicebus${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'servicebus'
  }
  sku: {
    name: sku
  }
  properties: {
  }
}

resource roleAssignment_zLydyz7xm 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBusNamespace_eRbchjzJN
  name: guid(serviceBusNamespace_eRbchjzJN.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalId: principalId
    principalType: principalType
  }
}

resource serviceBusQueue_YGyPfTH4Y 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: serviceBusNamespace_eRbchjzJN
  name: 'queue1'
  location: location
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 5
  }
}

resource serviceBusTopic_1PfQC0XCu 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_eRbchjzJN
  name: 'topic1'
  location: location
  properties: {
    enablePartitioning: true
  }
}

resource serviceBusSubscription_CRhdrb3WU 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_1PfQC0XCu
  name: 'subscription1'
  location: location
  properties: {
    lockDuration: 'PT5M'
    requiresSession: true
  }
}

resource serviceBusSubscription_4bNfuM8dY 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_1PfQC0XCu
  name: 'subscription2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_Bc2arm6yg 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_eRbchjzJN
  name: 'topic2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_5Lcroh5WO 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_eRbchjzJN
  name: 'topic3'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_Sc1OgRrIK 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_5Lcroh5WO
  name: 'sub1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_R6GJEiXWz 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_5Lcroh5WO
  name: 'sub2'
  location: location
  properties: {
  }
}

output serviceBusEndpoint string = serviceBusNamespace_eRbchjzJN.properties.serviceBusEndpoint
