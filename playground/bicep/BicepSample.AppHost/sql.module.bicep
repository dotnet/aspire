targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalName string


resource sqlServer_l5O9GRsSn 'Microsoft.Sql/servers@2020-11-01-preview' = {
  name: toLower(take(concat('sql', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'sql'
  }
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
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

resource sqlFirewallRule_Kr30BcxQt 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
  parent: sqlServer_l5O9GRsSn
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase_A20agRiP6 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
  parent: sqlServer_l5O9GRsSn
  name: 'db'
  location: location
  properties: {
  }
}

output sqlServerFqdn string = sqlServer_l5O9GRsSn.properties.fullyQualifiedDomainName
