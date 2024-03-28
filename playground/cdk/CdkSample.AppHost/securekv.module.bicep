targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param signaturesecret string


resource keyVault_brAPeys65 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: toLower(take(concat('securekv', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'securekv'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
    createMode: 'recover'
  }
}

resource roleAssignment_dH6ucCy4w 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault_brAPeys65
  name: guid(keyVault_brAPeys65.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: principalId
    principalType: principalType
  }
}

resource keyVaultSecret_isTm9TWCm 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_brAPeys65
  name: 'mysecret'
  location: location
  properties: {
    value: signaturesecret
  }
}

output vaultUri string = keyVault_brAPeys65.properties.vaultUri
