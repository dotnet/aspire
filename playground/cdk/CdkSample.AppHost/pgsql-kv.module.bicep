@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource pgsql_kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('pgsqlkv-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'pgsql-kv'
  }
}

resource pgsql_kv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(pgsql_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalType: principalType
  }
  scope: pgsql_kv
}

output vaultUri string = pgsql_kv.properties.vaultUri

output name string = pgsql_kv.name

output id string = pgsql_kv.id