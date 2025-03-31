@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param account_kv_outputs_name string

param principalId string

resource account_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: account_kv_outputs_name
}

resource account_kv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalType: 'ServicePrincipal'
  }
  scope: account_kv
}