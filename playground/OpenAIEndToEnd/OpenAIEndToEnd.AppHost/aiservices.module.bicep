@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource aiservices_hub 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: take('aiserviceshub${uniqueString(resourceGroup().id)}', 24)
  location: location
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'aiservices'
    hbiWorkspace: false
    v1LegacyMode: false
    publicNetworkAccess: 'Enabled'
  }
}

resource aiservices_project 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: take('aiservicesproject${uniqueString(resourceGroup().id)}', 24)
  location: location
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'aiservices'
    hubResourceId: aiservices_hub.id
    publicNetworkAccess: 'Enabled'
  }
}

resource aiservices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: take('aiservices${uniqueString(resourceGroup().id)}', 64)
  location: location
  kind: 'AIServices'
  properties: {
    customSubDomainName: toLower(take(concat('aiservices', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
  sku: {
    name: 'S0'
  }
  tags: {
    'aspire-resource-name': 'aiservices'
  }
}

resource aiservices_f6c7c914_8db3_469d_8ca1_694a8f32e121 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiservices.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f6c7c914-8db3-469d-8ca1-694a8f32e121'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f6c7c914-8db3-469d-8ca1-694a8f32e121')
    principalType: principalType
  }
  scope: aiservices
}

resource aiservices_ea01e6af_a1c1_4350_9563_ad00f8c72ec5 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiservices.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5')
    principalType: principalType
  }
  scope: aiservices
}

resource aiservices_connection 'Microsoft.MachineLearningServices/workspaces/connections@2024-10-01' = {
  name: take('aiservices${uniqueString(resourceGroup().id)}', 64)
  parent: aiservices_hub
  properties: {
    category: 'AIServices'
    target: aiservices.properties.endpoint
    authType: 'ApiKey'
    isSharedToAll: true
    credentials: {
      key: aiservices.listKeys().key1
    }
    metadata: {
      ApiType: 'Azure'
      ResourceId: aiservices.id
    }
  }
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'Phi-4'
  properties: {
    model: {
      format: 'Microsoft'
      name: 'Phi-4'
      version: '3'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 1
  }
  parent: aiservices
}

output connectionString string = 'Endpoint=${aiservices.properties.endpoints['OpenAI Language Model Instance API']}'