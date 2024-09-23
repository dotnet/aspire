@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
  name: keyVaultName
}

resource redis 'Microsoft.Cache/redis@2020-06-01' = {
  name: take('redis-${uniqueString(resourceGroup().id)}', 63)
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
    'aspire-resource-name': 'redis'
  }
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: 'connectionString'
  properties: {
    value: '${redis.properties.hostName},ssl=true,password=${redis.listKeys().primaryKey}'
  }
  parent: keyVault
}