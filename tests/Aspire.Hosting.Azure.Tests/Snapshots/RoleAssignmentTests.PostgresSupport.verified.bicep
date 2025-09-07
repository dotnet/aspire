@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param postgres_outputs_name string

param principalId string

param principalName string

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: postgres_outputs_name
}

resource postgres_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  name: principalId
  properties: {
    principalName: principalName
    principalType: 'ServicePrincipal'
  }
  parent: postgres
}