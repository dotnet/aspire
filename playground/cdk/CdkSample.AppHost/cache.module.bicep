targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param keyVaultName string

@description('')
param principalId string

@description('')
param principalType string


resource redisCache_p9fE6TK3F 'Microsoft.Cache/Redis@2020-06-01' = {
  name: toLower(take(concat('cache', uniqueString(resourceGroup().id)), 24))
  location: location
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

resource keyVault_GLHqcGjrx 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
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

resource keyVaultSecret_00uTkXYQa 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_GLHqcGjrx
  name: 'connectionString'
  location: location
  properties: {
    value: '${redisCache_p9fE6TK3F.properties.hostName},ssl=true,password=${redisCache_p9fE6TK3F.listKeys(redisCache_p9fE6TK3F.apiVersion).primaryKey}'
  }
}

resource keyVaultSecret_SoyN6fZ8F 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_GLHqcGjrx
  name: 'keyVaultName'
  location: location
  properties: {
    value: keyVaultName
  }
}

resource keyVaultSecret_wGdvQ8DEM 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_GLHqcGjrx
  name: 'principalId'
  location: location
  properties: {
    value: principalId
  }
}

resource keyVaultSecret_cmuFkn6iw 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_GLHqcGjrx
  name: 'principalType'
  location: location
  properties: {
    value: principalType
  }
}
