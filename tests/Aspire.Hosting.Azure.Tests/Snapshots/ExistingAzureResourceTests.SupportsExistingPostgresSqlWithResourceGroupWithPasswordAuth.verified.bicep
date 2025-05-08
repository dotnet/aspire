﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

param administratorLogin string

@secure()
param administratorLoginPassword string

param keyVaultName string

resource postgresSql 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: existingResourceName
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: postgresSql
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--postgresSql'
  properties: {
    value: 'Host=${postgresSql.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
  }
  parent: keyVault
}

output name string = existingResourceName