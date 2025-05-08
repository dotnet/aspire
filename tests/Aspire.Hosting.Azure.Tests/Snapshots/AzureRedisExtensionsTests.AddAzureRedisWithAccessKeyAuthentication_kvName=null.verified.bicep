﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource redis_cache 'Microsoft.Cache/redis@2024-03-01' = {
  name: take('rediscache-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
  tags: {
    'aspire-resource-name': 'redis-cache'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--redis-cache'
  properties: {
    value: '${redis_cache.properties.hostName},ssl=true,password=${redis_cache.listKeys().primaryKey}'
  }
  parent: keyVault
}

output name string = redis_cache.name