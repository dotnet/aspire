targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param sku string = 'Standard'

@description('')
param principalId string

@description('')
param principalType string


resource serviceBusNamespace_1RzZvI0LZ 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: toLower(take('sb${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'sb'
  }
  sku: {
    name: sku
  }
  properties: {
  }
}

resource roleAssignment_GAWCqJpjI 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBusNamespace_1RzZvI0LZ
  name: guid(serviceBusNamespace_1RzZvI0LZ.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalId: principalId
    principalType: principalType
  }
}

resource serviceBusQueue_kQwbucWhl 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: serviceBusNamespace_1RzZvI0LZ
  name: 'queue1'
  location: location
  properties: {
  }
}

resource serviceBusTopic_768oqOlcX 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_1RzZvI0LZ
  name: 'topic1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_IcxQHWZBG 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_768oqOlcX
  name: 'subscription1'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_exANvItuE 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_768oqOlcX
  name: 'subscription2'
  location: location
  properties: {
  }
}

resource serviceBusTopic_nemvFxmjZ 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: serviceBusNamespace_1RzZvI0LZ
  name: 'topic2'
  location: location
  properties: {
  }
}

resource serviceBusSubscription_qiv2k0Nuu 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: serviceBusTopic_nemvFxmjZ
  name: 'subscription1'
  location: location
  properties: {
  }
}

output serviceBusEndpoint string = serviceBusNamespace_1RzZvI0LZ.properties.serviceBusEndpoint
