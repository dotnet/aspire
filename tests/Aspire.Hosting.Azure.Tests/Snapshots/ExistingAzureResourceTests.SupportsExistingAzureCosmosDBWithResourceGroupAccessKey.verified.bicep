@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

param keyVaultName string

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: existingResourceName
}

resource mydb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'mydb'
  location: location
  properties: {
    resource: {
      id: 'mydb'
    }
  }
  parent: cosmos
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'container'
  location: location
  properties: {
    resource: {
      id: 'container'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: mydb
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--cosmos'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
  }
  parent: keyVault
}

resource mydb_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--mydb'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydb'
  }
  parent: keyVault
}

resource container_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--container'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydb;Container=container'
  }
  parent: keyVault
}

output name string = existingResourceName