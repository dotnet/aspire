@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

@secure()
param signaturesecret string

resource mykv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('mykv-${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'mykv'
  }
}

resource mykv_KeyVaultAdministrator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mykv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalType: principalType
  }
  scope: mykv
}

resource mysecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'mysecret'
  properties: {
    value: signaturesecret
  }
  parent: mykv
}

output vaultUri string = mykv.properties.vaultUri