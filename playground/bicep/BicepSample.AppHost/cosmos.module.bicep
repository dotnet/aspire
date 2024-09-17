@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
    name: keyVaultName
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15-preview' = {
    name: toLower(take('cosmos${uniqueString(resourceGroup().id)}', 24))
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
        'aspire-resource-name': 'cosmos'
    }
}

resource db3 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15-preview' = {
    name: 'db3'
    location: location
    properties: {
        resource: {
            id: 'db3'
        }
    }
    parent: cosmos
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
    name: 'connectionString'
    properties: {
        value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
    }
    parent: keyVault
}