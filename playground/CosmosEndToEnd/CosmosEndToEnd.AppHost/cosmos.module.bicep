@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
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
    'aspire-resource-name': 'cosmos'
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
  parent: cosmos
}

resource entries 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'staging-entries'
  location: location
  properties: {
    resource: {
      id: 'staging-entries'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: db
}

resource users 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'users'
  location: location
  properties: {
    resource: {
      id: 'users'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: db
}

resource user_todo 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'UserTodo'
  location: location
  properties: {
    resource: {
      id: 'UserTodo'
      partitionKey: {
        paths: [
          '/userId'
          '/id'
        ]
      }
    }
  }
  parent: db
}

output connectionString string = cosmos.properties.documentEndpoint

output name string = cosmos.name