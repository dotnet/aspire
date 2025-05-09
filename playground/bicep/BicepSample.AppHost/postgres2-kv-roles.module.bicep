@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param postgres2_kv_outputs_name string

param principalType string

param principalId string

resource postgres2_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: postgres2_kv_outputs_name
}

resource postgres2_kv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(postgres2_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalType: principalType
  }
  scope: postgres2_kv
}