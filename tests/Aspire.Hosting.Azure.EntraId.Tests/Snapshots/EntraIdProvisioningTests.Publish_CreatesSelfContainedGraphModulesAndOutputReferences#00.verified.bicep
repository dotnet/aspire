targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module entra_api 'entra-api/entra-api.bicep' = {
  name: 'entra-api'
  scope: rg
  params: {
    location: location
    applicationName: 'entra-api'
    principalId: principalId
    signInAudience: 'AzureADMyOrg'
  }
}

module entra_web 'entra-web/entra-web.bicep' = {
  name: 'entra-web'
  scope: rg
  params: {
    location: location
    applicationName: 'entra-web'
    principalId: principalId
    signInAudience: 'AzureADMultipleOrgs'
    redirectUris: [
      'https://localhost:7123/signin-oidc'
    ]
  }
}

module consumer 'consumer/consumer.bicep' = {
  name: 'consumer'
  scope: rg
  params: {
    location: location
    tenantId: entra_web.outputs.tenantId
    clientId: entra_web.outputs.clientId
  }
}