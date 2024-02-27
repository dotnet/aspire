@description('West US 3')
param location string

@description('')
param tableUri string


resource keyVault_Mc6LeAH6Q 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'jane-temp'
  location: location
  properties: {
    tenantId: '5f85e12e-4fd7-475a-8acf-b6f07b4ff684'
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
  }
}

resource keyVaultSecret_ZTFvs0PgN 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_Mc6LeAH6Q
  name: 'mysecret-temp'
  properties: {
    value: tableUri
  }
}

output vaultUri string = keyVault_Mc6LeAH6Q.properties.vaultUri
