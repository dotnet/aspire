@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param redis_cache_outputs_name string

param principalId string

param principalName string

resource redis_cache 'Microsoft.Cache/redis@2024-03-01' existing = {
  name: redis_cache_outputs_name
}

resource redis_cache_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
  name: guid(redis_cache.id, principalId, 'Data Contributor')
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: principalId
    objectIdAlias: principalName
  }
  parent: redis_cache
}
