@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalName string

resource sql1 'Microsoft.Sql/servers@2021-11-01' = {
  name: take('sql1-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: principalName
      sid: principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
  tags: {
    'aspire-resource-name': 'sql1'
  }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: sql1
}

resource db1 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'db1'
  location: location
  parent: sql1
}

output sqlServerFqdn string = sql1.properties.fullyQualifiedDomainName

output name string = sql1.name