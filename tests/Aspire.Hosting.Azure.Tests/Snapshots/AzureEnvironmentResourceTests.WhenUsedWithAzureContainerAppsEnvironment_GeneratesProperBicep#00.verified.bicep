targetScope = 'subscription'

param azure_rg_default string

param azure_location_default string

param principalId string

var tags = {
  'aspire-env-name': azure_rg_default
}

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: azure_rg_default
  location: azure_location_default
  tags: tags
}

module env 'env/env.bicep' = {
  name: 'env'
  scope: rg
  params: {
    location: azure_location_default
    userPrincipalId: principalId
  }
}

output env_AZURE_CONTAINER_REGISTRY_NAME string = env.outputs.AZURE_CONTAINER_REGISTRY_NAME

output env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID