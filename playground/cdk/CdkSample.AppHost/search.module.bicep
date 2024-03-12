targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource searchService_7WkaGluF0 'Microsoft.Search/searchServices@2023-11-01' = {
  name: toLower(take(concat('search', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'search'
  }
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    disableLocalAuth: true
  }
}

resource roleAssignment_7uytIREoa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: searchService_7WkaGluF0
  name: guid(searchService_7WkaGluF0.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_QpFzCj55x 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: searchService_7WkaGluF0
  name: guid(searchService_7WkaGluF0.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
    principalId: principalId
    principalType: principalType
  }
}

output connectionString string = 'Endpoint=https://${searchService_7WkaGluF0.name}.search.windows.net'
