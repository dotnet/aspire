@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param administratorLogin string

@secure()
param administratorLoginPassword string

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
    name: keyVaultName
}

resource pgsql2 'Microsoft.DBforPostgreSQL/flexibleServers@2022-12-01' = {
    name: toLower(take('pgsql2${uniqueString(resourceGroup().id)}', 24))
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
        'aspire-resource-name': 'pgsql2'
    }
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2022-12-01' = {
    name: 'AllowAllAzureIps'
    properties: {
        endIpAddress: '0.0.0.0'
        startIpAddress: '0.0.0.0'
    }
    parent: pgsql2
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
    name: 'connectionString'
    properties: {
        value: 'Host=${pgsql2.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
    }
    parent: keyVault
}