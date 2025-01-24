@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalType string

param principalId string

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
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
    disableLocalAuth: true
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'account'
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
  parent: account
}

resource account_DocumentDBAccountContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5bd9cd88-fe45-4216-938b-f97437e15450'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5bd9cd88-fe45-4216-938b-f97437e15450')
    principalType: principalType
  }
  scope: account
}

output connectionString string = account.properties.documentEndpoint