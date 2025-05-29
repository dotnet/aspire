﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

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
    disableLocalAuth: false
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'cosmos'
  }
}

resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'mydatabase'
  location: location
  properties: {
    resource: {
      id: 'mydatabase'
    }
  }
  parent: cosmos
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'mycontainer'
  location: location
  properties: {
    resource: {
      id: 'mycontainer'
      partitionKey: {
        paths: [
          'mypartitionkeypath'
        ]
      }
    }
  }
  parent: db
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

resource db_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--db'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydatabase'
  }
  parent: keyVault
}

resource container_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--container'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=mydatabase;Container=mycontainer'
  }
  parent: keyVault
}

output name string = cosmos.name