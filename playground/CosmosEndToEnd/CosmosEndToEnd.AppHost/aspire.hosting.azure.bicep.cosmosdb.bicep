param databaseAccountName string
param keyVaultName string

param databases array = []

@description('Tags that will be applied to all resources')
param tags object = {}

param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
    name: replace('${databaseAccountName}-${resourceToken}', '-', '')
    location: location
    kind: 'GlobalDocumentDB'
    tags: tags
    properties: {
        consistencyPolicy: { defaultConsistencyLevel: 'Session' }
        locations: [
            {
                locationName: location
                failoverPriority: 0
            }
        ]
        databaseAccountOfferType: 'Standard'
    }

    resource db 'sqlDatabases@2023-04-15' = [for name in databases: {
        name: '${name}'
        location: location
        tags: tags
        properties: {
            resource: {
                id: '${name}'
            }
        }
    }]
}

var primaryMasterKey = cosmosDb.listKeys(cosmosDb.apiVersion).primaryMasterKey

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
    name: keyVaultName

    resource secret 'secrets@2023-07-01' = {
        name: 'connectionString'
        properties: {
            value: 'AccountEndpoint=${cosmosDb.properties.documentEndpoint};AccountKey=${primaryMasterKey}'
        }
    }
}

