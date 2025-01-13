@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

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
  name: 'entries'
  location: location
  properties: {
    resource: {
      id: 'entries'
      partitionKey: {
        paths: [
          '/Id'
        ]
      }
    }
  }
  parent: db
}

output connectionString string = cosmos.properties.documentEndpoint