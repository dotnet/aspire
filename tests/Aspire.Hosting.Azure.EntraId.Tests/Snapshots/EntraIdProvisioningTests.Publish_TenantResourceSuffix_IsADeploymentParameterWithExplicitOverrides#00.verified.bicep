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
  }
}

module entra_api 'entra-api/entra-api.bicep' = {
  name: 'entra-api'
  scope: rg
  params: {
    location: location
    applicationName: 'entra-api'
    principalId: principalId
    signInAudience: 'AzureADMyOrg'
    tenantResourceSuffix: 'production'
  }
}