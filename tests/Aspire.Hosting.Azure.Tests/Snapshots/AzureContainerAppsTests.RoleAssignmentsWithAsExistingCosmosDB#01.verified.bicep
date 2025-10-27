@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param cosmosName string

param principalId string

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: cosmosName
}

resource cosmos_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: cosmos
}

resource cosmos_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  name: guid(principalId, cosmos_roleDefinition.id, cosmos.id)
  properties: {
    principalId: principalId
    roleDefinitionId: cosmos_roleDefinition.id
    scope: cosmos.id
  }
  parent: cosmos
}