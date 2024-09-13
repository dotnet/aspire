targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param administratorLogin string

@secure()
@description('')
param administratorLoginPassword string

@description('')
param keyVaultName string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource postgreSqlFlexibleServer_lEvmXPNkk 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: toLower(take('pg${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'pg'
  }
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '16'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    availabilityZone: '1'
  }
}

resource postgreSqlFirewallRule_MQ1aepXRF 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgreSqlFlexibleServer_lEvmXPNkk
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource postgreSqlFlexibleServerDatabase_njyTxNuMP 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgreSqlFlexibleServer_lEvmXPNkk
  name: 'db'
  properties: {
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: 'Host=${postgreSqlFlexibleServer_lEvmXPNkk.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
  }
}
