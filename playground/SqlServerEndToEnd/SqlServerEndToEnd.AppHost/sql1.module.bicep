targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalName string


resource sqlServer_MXlkl0TrE 'Microsoft.Sql/servers@2020-11-01-preview' = {
  name: toLower(take(concat('sql1', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'sql1'
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

resource sqlFirewallRule_zucOewiTI 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
  parent: sqlServer_MXlkl0TrE
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase_pLQwSRl2h 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
  parent: sqlServer_MXlkl0TrE
  name: 'db1'
  location: location
  properties: {
  }
}

output sqlServerFqdn string = sqlServer_MXlkl0TrE.properties.fullyQualifiedDomainName
