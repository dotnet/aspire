@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param pg_kv_outputs_name string

param principalType string

param principalId string

resource pg_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: pg_kv_outputs_name
}

resource pg_kv_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(pg_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: principalType
  }
  scope: pg_kv
}