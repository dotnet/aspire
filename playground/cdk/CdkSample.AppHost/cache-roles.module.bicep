@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param cache_outputs_name string

param principalId string

param principalName string

resource cache 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: cache_outputs_name
}

resource cache_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-11-01' = {
  name: guid(cache.id, principalId, 'Data Contributor')
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: principalId
    objectIdAlias: principalName
  }
  parent: cache
}