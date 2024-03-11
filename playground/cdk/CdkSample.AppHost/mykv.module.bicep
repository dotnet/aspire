targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param signaturesecret string


resource keyVault_IKWI2x0B5 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: toLower(take(concat('mykv', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'mykv'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
  }
}

resource roleAssignment_Z4xb36awa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault_IKWI2x0B5
  name: guid(keyVault_IKWI2x0B5.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: principalId
    principalType: principalType
  }
}

resource keyVaultSecret_7ClrhkRcM 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_IKWI2x0B5
  name: 'mysecret'
  location: location
  properties: {
    value: signaturesecret
  }
}

output vaultUri string = keyVault_IKWI2x0B5.properties.vaultUri
