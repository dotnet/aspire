targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param keyVaultName string

@description('')
param principalId string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource redisCache_N8pcNBLX8 'Microsoft.Cache/Redis@2020-06-01' = {
  name: toLower(take(concat('redis', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'redis'
  }
  properties: {
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: '${redisCache_N8pcNBLX8.properties.hostName},ssl=true,password=${redisCache_N8pcNBLX8.listKeys(redisCache_N8pcNBLX8.apiVersion).primaryKey}'
  }
}
