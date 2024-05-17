targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param keyVaultName string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource cosmosDBAccount_MZyw35gqp 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: toLower(take('cosmos${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'cosmos'
  }
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: 'AccountEndpoint=${cosmosDBAccount_MZyw35gqp.properties.documentEndpoint};AccountKey=${cosmosDBAccount_MZyw35gqp.listkeys(cosmosDBAccount_MZyw35gqp.apiVersion).primaryMasterKey}'
  }
}
