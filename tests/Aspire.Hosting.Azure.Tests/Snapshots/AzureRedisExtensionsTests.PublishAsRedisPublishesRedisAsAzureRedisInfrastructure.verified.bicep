@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource cache 'Microsoft.Cache/redis@2024-03-01' = {
  name: take('cache-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
  tags: {
    'aspire-resource-name': 'cache'
  }
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionString'
  properties: {
    value: '${cache.properties.hostName},ssl=true,password=${cache.listKeys().primaryKey}'
  }
  parent: keyVault
}