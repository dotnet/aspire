@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sql_outputs_name string

param principalId string

param principalName string

resource sql 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: sql_outputs_name
}

resource sql_admin 'Microsoft.Sql/servers/administrators@2021-11-01' = {
  name: 'ActiveDirectory'
  properties: {
    login: principalName
    sid: principalId
  }
  parent: sql
}