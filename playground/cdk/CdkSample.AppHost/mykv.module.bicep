targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param signaturesecret string


resource keyVault_aMZbuK3Sy 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: toLower(take('mykv${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'mykv'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
}

resource roleAssignment_hVU9zjQV1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault_aMZbuK3Sy
  name: guid(keyVault_aMZbuK3Sy.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: principalId
    principalType: principalType
  }
}

resource keyVaultSecret_mW5tlkNij 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_aMZbuK3Sy
  name: 'mysecret'
  location: location
  properties: {
    value: signaturesecret
  }
}

output vaultUri string = keyVault_aMZbuK3Sy.properties.vaultUri
