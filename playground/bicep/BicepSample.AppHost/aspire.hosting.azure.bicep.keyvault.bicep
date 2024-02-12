param vaultName string
param principalId string

@description('Tags that will be applied to all resources')
param tags object = {}

param principalType string = 'ServicePrincipal'

param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: replace('${vaultName}-${resourceToken}', '-', '')
  location: location
  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
  tags: tags
}

resource KeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  scope: vault
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
  }
}

output vaultUri string = vault.properties.vaultUri
