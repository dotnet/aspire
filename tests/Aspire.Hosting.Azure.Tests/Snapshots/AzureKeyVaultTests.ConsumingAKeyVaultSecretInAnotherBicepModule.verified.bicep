@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param mykeyvault_outputs_name string

resource mykeyvault_outputs_name_kv 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: mykeyvault_outputs_name
}

resource mykeyvault_outputs_name_kv_mySecret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'mySecret'
  parent: mykeyvault_outputs_name_kv
}

resource mykeyvault_outputs_name_kv_mySecret2 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'mySecret2'
  parent: mykeyvault_outputs_name_kv
}

output secretUri1 string = mykeyvault_outputs_name_kv_mySecret.properties.secretUri

output secretUri2 string = mykeyvault_outputs_name_kv_mySecret2.properties.secretUri