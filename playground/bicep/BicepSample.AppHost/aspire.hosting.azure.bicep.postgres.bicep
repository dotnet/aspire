param administratorLogin string
param keyVaultName string

@secure()
param administratorLoginPassword string
param location string = resourceGroup().location
param serverName string
param serverEdition string = 'Burstable'
param skuSizeGB int = 32
param dbInstanceType string = 'Standard_B1ms'
param haMode string = 'Disabled'
param availabilityZone string = '1'
param version string = '16'
param databases array = []

var resourceToken = uniqueString(resourceGroup().id)

resource pgserver 'Microsoft.DBforPostgreSQL/flexibleServers@2021-06-01' = {
    name: '${serverName}-${resourceToken}'
    location: location
    sku: {
        name: dbInstanceType
        tier: serverEdition
    }
    properties: {
        version: version
        administratorLogin: administratorLogin
        administratorLoginPassword: administratorLoginPassword
        network: {
            delegatedSubnetResourceId: null
            privateDnsZoneArmResourceId: null
        }
        highAvailability: {
            mode: haMode
        }
        storage: {
            storageSizeGB: skuSizeGB
        }
        backup: {
            backupRetentionDays: 7
            geoRedundantBackup: 'Disabled'
        }
        availabilityZone: availabilityZone
    }

    resource firewallRules 'firewallRules@2021-06-01' = {
        name: 'fw-pg-localdev'
        properties: {
            startIpAddress: '0.0.0.0'
            endIpAddress: '255.255.255.255'
        }
    }

    resource database 'databases@2021-06-01' = [for name in databases: {
        name: name
    }]
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
    name: keyVaultName

    resource secret 'secrets@2023-07-01' = {
        name: 'connectionString'
        properties: {
            value: 'Host=${pgserver.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
        }
    }
}
 }
    }
}
