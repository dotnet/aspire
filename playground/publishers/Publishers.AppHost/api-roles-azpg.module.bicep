@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param azpg_outputs_name string

param principalId string

param principalName string

resource azpg 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: azpg_outputs_name
}

resource azpg_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  name: principalId
  properties: {
    principalName: principalName
    principalType: 'ServicePrincipal'
  }
  parent: azpg
}