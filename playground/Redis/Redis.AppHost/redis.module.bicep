@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource redis 'Microsoft.Cache/redisEnterprise@2025-04-01' = {
  name: take('redis-${uniqueString(resourceGroup().id)}', 60)
  location: location
  sku: {
    name: 'Balanced_B0'
  }
}

resource redis_default 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' = {
  name: 'default'
  properties: {
    port: 10000
  }
  parent: redis
}

output connectionString string = '${redis.properties.hostName}:10000,ssl=true'

output name string = redis.name