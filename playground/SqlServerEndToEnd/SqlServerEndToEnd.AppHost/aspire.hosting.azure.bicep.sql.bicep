@description('User name')
param principalName string 

@description('User id')
param principalId string 

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('The name of the sql server resource')
param serverName string

param databases array = []

var resourceToken = uniqueString(resourceGroup().id)

resource sql 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: '${serverName}-${resourceToken}'
  location: location
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: principalName
      principalType: 'User'
      sid: principalId
      tenantId: subscription().tenantId
    }
  }

resource sqlFirewall 'firewallRules@2022-05-01-preview' = {
    name: 'fw-sql-localdev'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '255.255.255.255'
    }
}

  resource db 'databases@2022-05-01-preview' = [for name in databases:{
  name: name
  location: location
  sku: {
    name: 'S0'
  }
  tags: tags
  }]

}

output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName
