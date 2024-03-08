targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param keyVaultName string

@description('')
param principalId string

@description('')
param principalName string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

resource cosmosDBAccount_9B2tZtguY 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: toLower(take(concat('mongo', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'mongo'
  }
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: 'westus'
      }
    ]
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: 'AccountEndpoint=${cosmosDBAccount_9B2tZtguY.properties.documentEndpoint};AccountKey=${cosmosDBAccount_9B2tZtguY.listkeys(cosmosDBAccount_9B2tZtguY.apiVersion).primaryMasterKey}'
  }
}
