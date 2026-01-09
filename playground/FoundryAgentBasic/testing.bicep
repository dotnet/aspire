

resource aaatesting123 'Microsoft.CognitiveServices/accounts@2025-10-01-preview' = {
  name: 'aaatesting123'
  location: 'swedencentral'
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    allowProjectManagement: true
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    customSubDomainName: 'aaatesting123'
  }
}
