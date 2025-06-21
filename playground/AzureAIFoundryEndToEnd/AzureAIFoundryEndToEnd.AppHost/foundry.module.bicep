@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource foundry 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: take('foundry${uniqueString(resourceGroup().id)}', 64)
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'AIServices'
  properties: {
    customSubDomainName: toLower(take(concat('foundry', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
  sku: {
    name: 'S0'
  }
  tags: {
    'aspire-resource-name': 'foundry'
  }
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'qwen2.5-0.5b'
  properties: {
    model: {
      format: 'Microsoft'
      name: 'qwen2.5-0.5b'
      version: '1'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 1
  }
  parent: foundry
}

output aiFoundryApiEndpoint string = foundry.properties.endpoints['AI Foundry API']