@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource foundry 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: take('foundry${uniqueString(resourceGroup().id)}', 64)
  location: location
  kind: 'AIServices'
  properties: {
    customSubDomainName: toLower(take(concat('foundry', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
  sku: {
    name: 'S0'
  }
  tags: {
    'aspire-resource-name': 'foundry'
  }
}

resource foundry_f6c7c914_8db3_469d_8ca1_694a8f32e121 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundry.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f6c7c914-8db3-469d-8ca1-694a8f32e121'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f6c7c914-8db3-469d-8ca1-694a8f32e121')
    principalType: principalType
  }
  scope: foundry
}

resource foundry_ea01e6af_a1c1_4350_9563_ad00f8c72ec5 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundry.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5')
    principalType: principalType
  }
  scope: foundry
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'Phi-4'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'Phi-4'
      version: '2024-05-13'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 1
  }
  parent: foundry
}

output connectionString string = 'Endpoint=${foundry.properties.endpoints['OpenAI Language Model Instance API']}'