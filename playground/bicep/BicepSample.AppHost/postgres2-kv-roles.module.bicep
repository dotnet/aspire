@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param postgres2_kv_outputs_name string

param principalType string

param principalId string

resource postgres2_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: postgres2_kv_outputs_name
}

resource postgres2_kv_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(postgres2_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: principalType
  }
  scope: postgres2_kv
}