targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

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

resource keyVaultSecret_dPFd3FfoI 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_OlyAsQ0DX
  name: 'mysecret'
  location: location
  properties: {
    value: signaturesecret
  }
}

output vaultUri string = keyVault_OlyAsQ0DX.properties.vaultUri
