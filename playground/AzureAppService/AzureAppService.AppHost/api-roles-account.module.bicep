@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param account_outputs_name string

param principalId string

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: account_outputs_name
}

resource account_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: account
}

resource account_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  name: guid(principalId, account_roleDefinition.id, account.id)
  properties: {
    principalId: principalId
    roleDefinitionId: account_roleDefinition.id
    scope: account.id
  }
  parent: account
}