param administratorLogin string

@secure()
param administratorLoginPassword string

param location string = resourceGroup().location

resource pgsql 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
    name: take('pgsql${uniqueString(resourceGroup().id)}', 24)
    location: location
    properties: {
        administratorLogin: administratorLogin
        administratorLoginPassword: administratorLoginPassword
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
        'aspire-resource-name': 'pgsql'
    }
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
    name: 'AllowAllAzureIps'
    properties: {
        endIpAddress: '0.0.0.0'
        startIpAddress: '0.0.0.0'
    }
    parent: pgsql
}

resource pgsqldb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
    name: 'pgsqldb'
    parent: pgsql
}

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
    name: keyVaultName
    location: resourceGroup().location
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
    name: 'connectionString'
    properties: {
        value: 'Host=${pgsql.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
    }
    parent: keyVault
}