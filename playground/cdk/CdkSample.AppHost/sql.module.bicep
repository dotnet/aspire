targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalName string

@description('')
param principalType string


resource sqlServer_l5O9GRsSn 'Microsoft.Sql/servers@2022-08-01-preview' = {
  name: toLower(take(concat('sql', uniqueString(resourceGroup().id)), 24))
  location: location
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: principalType
      login: principalName
      sid: principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlFirewallRule_fA0ew2DcB 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
  parent: sqlServer_l5O9GRsSn
  name: 'fw'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '255.255.255.255'
  }
}

resource sqlDatabase_A20agRiP6 'Microsoft.Sql/servers/databases@2022-08-01-preview' = {
  parent: sqlServer_l5O9GRsSn
  name: 'db'
  location: location
  properties: {
    maxSizeBytes: 100000000
  }
}

output sqlServerFqdn string = sqlServer_l5O9GRsSn.properties.fullyQualifiedDomainName
