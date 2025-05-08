﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource sqlServerAdminManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('sql_server-admin-${uniqueString(resourceGroup().id)}', 63)
  location: location
}

resource sql_server 'Microsoft.Sql/servers@2021-11-01' = {
  name: toLower(take('sql-server${uniqueString(resourceGroup().id)}', 24))
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: sqlServerAdminManagedIdentity.name
      sid: sqlServerAdminManagedIdentity.properties.principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
  tags: {
    'aspire-resource-name': 'sql-server'
  }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: sql_server
}

resource evadexdb 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'evadexdb'
  location: location
  parent: sql_server
}

output sqlServerFqdn string = sql_server.properties.fullyQualifiedDomainName

output name string = sql_server.name

output sqlServerAdminName string = sqlServerAdminManagedIdentity.name