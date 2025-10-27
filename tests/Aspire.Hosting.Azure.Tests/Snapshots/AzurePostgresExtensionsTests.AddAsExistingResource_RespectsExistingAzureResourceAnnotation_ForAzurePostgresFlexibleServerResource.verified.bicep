@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_postgres_name string

param existing_postgres_rg string

resource test_postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: existing_postgres_name
  scope: resourceGroup(existing_postgres_rg)
}