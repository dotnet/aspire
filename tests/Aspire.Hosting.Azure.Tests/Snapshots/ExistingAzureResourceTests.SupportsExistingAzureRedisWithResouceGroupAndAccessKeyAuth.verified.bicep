@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param redis_kv_outputs_name string

resource redis 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: 'existingResourceName'
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: redis_kv_outputs_name
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'connectionstrings--redis'
  properties: {
    value: '${redis.properties.hostName},ssl=true,password=${redis.listKeys().primaryKey}'
  }
  parent: keyVault
}

output name string = redis.name