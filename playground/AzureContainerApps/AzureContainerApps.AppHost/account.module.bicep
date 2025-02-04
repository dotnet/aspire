@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: take('account-${uniqueString(resourceGroup().id)}', 44)
  location: location
  properties: {
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: true
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'account'
  }
}

resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'db'
  location: location
  properties: {
    resource: {
      id: 'db'
    }
  }
  parent: account
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

output connectionString string = account.properties.documentEndpoint