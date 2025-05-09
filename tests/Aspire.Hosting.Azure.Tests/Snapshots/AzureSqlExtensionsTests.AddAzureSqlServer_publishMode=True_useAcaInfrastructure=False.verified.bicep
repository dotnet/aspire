﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource sqlServerAdminManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('sql-admin-${uniqueString(resourceGroup().id)}', 63)
  location: location
}

resource sql 'Microsoft.Sql/servers@2021-11-01' = {
  name: take('sql-${uniqueString(resourceGroup().id)}', 63)
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
    'aspire-resource-name': 'sql'
  }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: sql
}

resource db1 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'db1'
  location: location
  properties: {
    freeLimitExhaustionBehavior: 'AutoPause'
    useFreeLimit: true
  }
  sku: {
    name: 'GP_S_Gen5_2'
  }
  parent: sql
}

resource db2 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'db2Name'
  location: location
  properties: {
    freeLimitExhaustionBehavior: 'AutoPause'
    useFreeLimit: true
  }
  sku: {
    name: 'GP_S_Gen5_2'
  }
  parent: sql
}

resource db3 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'db3Name'
  location: location
  parent: sql
}

output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName

output name string = sql.name

output sqlServerAdminName string = sqlServerAdminManagedIdentity.name