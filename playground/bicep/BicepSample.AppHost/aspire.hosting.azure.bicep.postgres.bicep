param administratorLogin string

@secure()
param administratorLoginPassword string
param location string = resourceGroup().location
param serverName string
param serverEdition string = 'GeneralPurpose'
param skuSizeGB int = 128
param dbInstanceType string = 'Standard_D4ds_v4'
param haMode string = 'ZoneRedundant'
param availabilityZone string = '1'
param version string = '12'
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

  resource database 'databases@2021-06-01' = [for name in databases:{
    name: name
  }]

}

output pgfqdn string = pgserver.properties.fullyQualifiedDomainName
