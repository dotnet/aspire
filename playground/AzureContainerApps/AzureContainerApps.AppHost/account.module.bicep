@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
  name: keyVaultName
}

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-05-15-preview' = {
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
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'account'
  }
}

resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15-preview' = {
  name: 'db'
  location: location
  properties: {
    resource: {
      id: 'db'
    }
  }
  parent: account
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: 'connectionString'
  properties: {
    value: 'AccountEndpoint=${account.properties.documentEndpoint};AccountKey=${account.listKeys().primaryMasterKey}'
  }
  parent: keyVault
}