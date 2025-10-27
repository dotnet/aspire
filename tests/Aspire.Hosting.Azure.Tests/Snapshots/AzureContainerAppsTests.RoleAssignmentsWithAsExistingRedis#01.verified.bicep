@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalName string

resource redis 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: 'myredis'
}

resource redis_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-11-01' = {
  name: guid(redis.id, principalId, 'Data Contributor')
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: principalId
    objectIdAlias: principalName
  }
  parent: redis
}