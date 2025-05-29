@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param kv3_outputs_name string

param principalType string

param principalId string

resource kv3 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: kv3_outputs_name
}

resource kv3_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv3.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: principalType
  }
  scope: kv3
}