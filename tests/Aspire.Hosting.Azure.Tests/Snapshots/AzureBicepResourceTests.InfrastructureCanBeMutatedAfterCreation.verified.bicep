@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'kvs'
  properties: {
    value: '00000000-0000-0000-0000-000000000000'
  }
  parent: kv
}

output vaultUri string = kv.properties.vaultUri