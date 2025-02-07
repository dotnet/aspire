@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalName string

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: take('redis-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
    enableNonSslPort: false
    disableAccessKeyAuthentication: true
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'aad-enabled': 'true'
    }
  }
  tags: {
    'aspire-resource-name': 'redis'
  }
}

resource redis_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
  name: take('rediscontributor${uniqueString(resourceGroup().id)}', 24)
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: principalId
    objectIdAlias: principalName
  }
  parent: redis
}

output connectionString string = '${redis.properties.hostName},ssl=true'