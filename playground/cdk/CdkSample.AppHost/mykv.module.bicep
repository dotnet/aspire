targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param signaturesecret string


resource keyVault_OlyAsQ0DX 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: toLower(take(concat('mykv', uniqueString(resourceGroup().id)), 24))
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
  }
}

resource roleAssignment_Nu9msjS3H 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault_OlyAsQ0DX
  name: guid(keyVault_OlyAsQ0DX.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: principalId
    principalType: principalType
  }
}

resource keyVaultSecret_dPFd3FfoI 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_OlyAsQ0DX
  name: 'mysecret'
  location: location
  properties: {
    value: signaturesecret
  }
}

output vaultUri string = keyVault_OlyAsQ0DX.properties.vaultUri
