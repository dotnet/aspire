@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param pg_kv_outputs_name string

param principalType string

param principalId string

resource pg_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: pg_kv_outputs_name
}

resource pg_kv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(pg_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalType: principalType
  }
  scope: pg_kv
}