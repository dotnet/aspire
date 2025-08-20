@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_cosmosdb_name string

param existing_cosmosdb_rg string

resource test_cosmosdb 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: existing_cosmosdb_name
  scope: resourceGroup(existing_cosmosdb_rg)
}