@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource sqlServerAdminManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('mysqlserver-admin-${uniqueString(resourceGroup().id)}', 63)
  location: location
}

resource mysqlserver 'Microsoft.Sql/servers@2021-11-01' = {
  name: take('mysqlserver-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: take('mysqlserver-admin-${uniqueString(resourceGroup().id)}', 63)
      sid: sqlServerAdminManagedIdentity.properties.principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
  tags: {
    'aspire-resource-name': 'mysqlserver'
  }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: mysqlserver
}

resource todosdb 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'todosdb'
  location: location
  sku: {
    name: 'GP_S_Gen5_2'
  }
  parent: mysqlserver
}

output sqlServerFqdn string = mysqlserver.properties.fullyQualifiedDomainName

output name string = mysqlserver.name