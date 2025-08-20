@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_kv_name string

param existing_kv_rg string

resource test_keyvault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: existing_kv_name
  scope: resourceGroup(existing_kv_rg)
}