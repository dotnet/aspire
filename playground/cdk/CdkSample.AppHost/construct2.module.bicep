targetScope = 'resourceGroup'

@description('West US 3')
param location string

@description('')
param tableUri string


resource keyVault_kb5kSO8cv 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'jane-temp'
  location: location
  properties: {
    tenantId: '00000000-0000-0000-0000-000000000000'
    sku: {
      name: 'standard'
      family: 'A'
    }
    enableRbacAuthorization: true
  }
}

resource keyVaultSecret_BirhV1djm 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_kb5kSO8cv
  name: 'mysecret-temp'
  properties: {
    value: tableUri
  }
}

output vaultUri string = keyVault_kb5kSO8cv.properties.vaultUri
