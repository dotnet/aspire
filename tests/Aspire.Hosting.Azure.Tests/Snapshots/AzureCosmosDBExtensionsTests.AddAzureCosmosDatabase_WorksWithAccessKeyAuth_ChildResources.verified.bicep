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

resource db1 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'db1'
  location: location
  properties: {
    resource: {
      id: 'db1'
    }
  }
  parent: cosmos
}

resource container1 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'container1'
  location: location
  properties: {
    resource: {
      id: 'container1'
      partitionKey: {
        paths: [
          'id'
        ]
      }
    }
  }
  parent: db1
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

resource db1_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--db1'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db1'
  }
  parent: keyVault
}

resource container1_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--container1'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db1;Container=container1'
  }
  parent: keyVault
}

output name string = cosmos.name