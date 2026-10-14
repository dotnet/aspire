targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

param tenantResourceSuffix string = ''

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module entra_web 'entra-web/entra-web.bicep' = {
  name: 'entra-web'
  scope: rg
  params: {
    location: location
    applicationName: 'entra-web'
    principalId: principalId
    signInAudience: 'AzureADMyOrg'
    tenantResourceSuffix: tenantResourceSuffix
    redirectUris: [
      'https://localhost:7123/signin-oidc'
      'https://localhost:7123/signout-callback-oidc'
    ]
    enableIdTokenIssuance: true
  }
}