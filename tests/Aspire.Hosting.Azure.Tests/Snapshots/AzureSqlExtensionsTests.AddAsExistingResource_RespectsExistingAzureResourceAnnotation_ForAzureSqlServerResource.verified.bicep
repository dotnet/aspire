@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_sql_name string

resource test_sql 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: existing_sql_name
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: test_sql
}

resource sqlFirewallRule_AllowAllIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  name: 'AllowAllIps'
  properties: {
    endIpAddress: '255.255.255.255'
    startIpAddress: '0.0.0.0'
  }
  parent: test_sql
}

output sqlServerFqdn string = test_sql.properties.fullyQualifiedDomainName

output name string = existing_sql_name

output sqlServerAdminName string = test_sql.properties.administrators.login