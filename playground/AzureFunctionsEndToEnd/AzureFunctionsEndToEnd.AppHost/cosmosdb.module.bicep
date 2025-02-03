@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource cosmosdb 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: take('cosmosdb-${uniqueString(resourceGroup().id)}', 44)
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
    'aspire-resource-name': 'cosmosdb'
  }
}

resource mydatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'mydatabase'
  location: location
  properties: {
    resource: {
      id: 'mydatabase'
    }
  }
  parent: cosmosdb
}

resource mycontainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'mycontainer'
  location: location
  properties: {
    resource: {
      id: 'mycontainer'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: mydatabase
}

resource cosmosdb_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: cosmosdb
}

resource cosmosdb_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  name: guid(principalId, cosmosdb_roleDefinition.id, cosmosdb.id)
  properties: {
    principalId: principalId
    roleDefinitionId: cosmosdb_roleDefinition.id
    scope: cosmosdb.id
  }
  parent: cosmosdb
}

output connectionString string = cosmosdb.properties.documentEndpoint