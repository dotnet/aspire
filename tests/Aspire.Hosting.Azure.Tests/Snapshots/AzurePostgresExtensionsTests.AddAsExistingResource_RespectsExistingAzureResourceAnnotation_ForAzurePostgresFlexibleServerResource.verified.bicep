@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_postgres_name string

resource test_postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: existing_postgres_name
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: test_postgres
}

resource postgreSqlFirewallRule_AllowAllIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllIps'
  properties: {
    endIpAddress: '255.255.255.255'
    startIpAddress: '0.0.0.0'
  }
  parent: test_postgres
}

output connectionString string = 'Host=${test_postgres.properties.fullyQualifiedDomainName}'

output name string = existing_postgres_name