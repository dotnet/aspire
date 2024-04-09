targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalName string


resource sqlServer_x8iP8H24Z 'Microsoft.Sql/servers@2020-11-01-preview' = {
  name: toLower(take('sql1${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'sql1'
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

resource sqlFirewallRule_9yJsWRmBv 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
  parent: sqlServer_x8iP8H24Z
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase_9KOoL8JWT 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
  parent: sqlServer_x8iP8H24Z
  name: 'db1'
  location: location
  properties: {
  }
}

output sqlServerFqdn string = sqlServer_x8iP8H24Z.properties.fullyQualifiedDomainName
