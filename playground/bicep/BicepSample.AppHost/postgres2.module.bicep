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

resource postgreSqlFlexibleServer_R66wZLcrB 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: toLower(take('postgres2${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'postgres2'
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

resource postgreSqlFirewallRule_TAPXfjXFL 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgreSqlFlexibleServer_R66wZLcrB
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource postgreSqlFlexibleServerDatabase_QYMh86Ekp 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgreSqlFlexibleServer_R66wZLcrB
  name: 'db2'
  properties: {
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: 'Host=${postgreSqlFlexibleServer_R66wZLcrB.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
  }
}
