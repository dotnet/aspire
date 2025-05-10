﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: existingResourceName
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: sqlServer
}

resource sqlFirewallRule_AllowAllIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllIps'
  properties: {
    endIpAddress: '255.255.255.255'
    startIpAddress: '0.0.0.0'
  }
  parent: sqlServer
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

output name string = existingResourceName

output sqlServerAdminName string = sqlServer.properties.administrators.login