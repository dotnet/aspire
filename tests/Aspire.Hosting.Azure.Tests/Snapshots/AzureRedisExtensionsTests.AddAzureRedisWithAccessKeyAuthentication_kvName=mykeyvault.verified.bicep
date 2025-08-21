@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param mykeyvault_outputs_name string

resource redis_cache 'Microsoft.Cache/redis@2024-11-01' = {
  name: take('rediscache-${uniqueString(resourceGroup().id)}', 63)
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
    'aspire-resource-name': 'redis-cache'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: mykeyvault_outputs_name
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'connectionstrings--redis-cache'
  properties: {
    value: '${redis_cache.properties.hostName},ssl=true,password=${redis_cache.listKeys().primaryKey}'
  }
  parent: keyVault
}

output name string = redis_cache.name