@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_sql_name string

param existing_sql_rg string

resource test_sql 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: existing_sql_name
  scope: resourceGroup(existing_sql_rg)
}