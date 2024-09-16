@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
    name: keyVaultName
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
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
    }
    kind: 'GlobalDocumentDB'
    tags: {
        'aspire-resource-name': 'cosmos'
    }
}

resource db3 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
    name: 'db3'
    location: location
    properties: {
        resource: {
            id: 'db3'
        }
    }
    parent: cosmos
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
    name: 'connectionString'
    properties: {
        value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
    }
    parent: keyVault
}