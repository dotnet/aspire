@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

param principalName string

resource azpg 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: take('azpg-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
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
    'aspire-resource-name': 'azpg'
  }
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: azpg
}

resource azdb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'azdb'
  parent: azpg
}

resource azpg_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  name: principalId
  properties: {
    principalName: principalName
    principalType: principalType
  }
  parent: azpg
  dependsOn: [
    azpg
    postgreSqlFirewallRule_AllowAllAzureIps
  ]
}

output connectionString string = 'Host=${azpg.properties.fullyQualifiedDomainName};Username=${principalName}'