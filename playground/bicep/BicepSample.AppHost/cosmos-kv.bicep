param location string
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2022-02-01-preview' = {
  name: 'kv-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: []
  }
  tags: tags
}

output name string = keyVault.name