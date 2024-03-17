targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalName string

@description('')
param principalType string


resource sqlServer_lF9QWGqAt 'Microsoft.Sql/servers@2020-11-01-preview' = {
  name: toLower(take('sql${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'sql'
  }
  properties: {
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      login: principalName
      sid: principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlFirewallRule_vcw7qNn72 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
  parent: sqlServer_lF9QWGqAt
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase_d4KUJMPIF 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
  parent: sqlServer_lF9QWGqAt
  name: 'db'
  location: location
  properties: {
  }
}

output sqlServerFqdn string = sqlServer_lF9QWGqAt.properties.fullyQualifiedDomainName
