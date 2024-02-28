@description('West US 3')
param location string

@description('')
param tableUri string


resource keyVault_jIQqamCos 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'jane-temp'
  location: location
  properties: {
    tenantId: '83da9385-a5f2-44d6-8190-6d0c4184a19a'
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
  }
}

resource keyVaultSecret_BJ0gguPoz 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_jIQqamCos
  name: 'mysecret-temp'
  properties: {
    value: tableUri
  }
}

output vaultUri string = keyVault_jIQqamCos.properties.vaultUri
