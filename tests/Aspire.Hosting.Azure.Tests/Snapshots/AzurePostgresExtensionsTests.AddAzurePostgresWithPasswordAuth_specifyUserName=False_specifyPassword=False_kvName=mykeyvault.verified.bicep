﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param administratorLogin string

@secure()
param administratorLoginPassword string

param keyVaultName string

resource postgres_data 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: take('postgresdata-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
    availabilityZone: '1'
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    storage: {
      storageSizeGB: 32
    }
    version: '16'
  }
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  tags: {
    'aspire-resource-name': 'postgres-data'
  }
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: postgres_data
}

resource postgreSqlFirewallRule_AllowAllIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllIps'
  properties: {
    endIpAddress: '255.255.255.255'
    startIpAddress: '0.0.0.0'
  }
  parent: postgres_data
}

resource db1 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'db1Name'
  parent: postgres_data
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--postgres-data'
  properties: {
    value: 'Host=${postgres_data.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
  }
  parent: keyVault
}

resource db1_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--db1'
  properties: {
    value: 'Host=${postgres_data.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword};Database=db1Name'
  }
  parent: keyVault
}

output name string = postgres_data.name