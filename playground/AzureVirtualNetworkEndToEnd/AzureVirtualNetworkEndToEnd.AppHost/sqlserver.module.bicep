@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource sqlServerAdminManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('sqlserver-admin-${uniqueString(resourceGroup().id)}', 63)
  location: location
}

resource sqlserver 'Microsoft.Sql/servers@2023-08-01' = {
  name: take('sqlserver-${uniqueString(resourceGroup().id)}', 63)
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
    publicNetworkAccess: 'Disabled'
    version: '12.0'
  }
  tags: {
    'aspire-resource-name': 'sqlserver'
  }
}

resource sqldb 'Microsoft.Sql/servers/databases@2023-08-01' = {
  name: 'sqldb'
  location: location
  properties: {
    freeLimitExhaustionBehavior: 'AutoPause'
    useFreeLimit: true
  }
  sku: {
    name: 'GP_S_Gen5_2'
  }
  parent: sqlserver
}

output sqlServerFqdn string = sqlserver.properties.fullyQualifiedDomainName

output name string = sqlserver.name

output id string = sqlserver.id

output sqlServerAdminName string = sqlserver.properties.administrators.login