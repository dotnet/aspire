@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param pgsql2_outputs_name string

param principalType string

param principalId string

param principalName string

resource pgsql2 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: pgsql2_outputs_name
}

resource pgsql2_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  name: principalId
  properties: {
    principalName: principalName
    principalType: principalType
  }
  parent: pgsql2
}