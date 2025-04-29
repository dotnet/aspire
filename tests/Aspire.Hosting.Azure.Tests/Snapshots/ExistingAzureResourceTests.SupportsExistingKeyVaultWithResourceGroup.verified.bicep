@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: existingResourceName
}

output vaultUri string = keyVault.properties.vaultUri

output name string = existingResourceName