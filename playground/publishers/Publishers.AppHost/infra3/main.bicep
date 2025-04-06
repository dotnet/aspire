targetScope = 'subscription'

param environmentName string

param location string

param principalId string

var tags = {
  'aspire-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module env 'env/env.bicep' = {
  name: 'env'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}

module azpg 'azpg/azpg.bicep' = {
  name: 'azpg'
  scope: rg
  params: {
    location: location
  }
}

module api_identity 'api-identity/api-identity.bicep' = {
  name: 'api-identity'
  scope: rg
  params: {
    location: location
  }
}

module api_roles_azpg 'api-roles-azpg/api-roles-azpg.bicep' = {
  name: 'api-roles-azpg'
  scope: rg
  params: {
    location: location
    azpg_outputs_name: azpg.outputs.name
    principalId: api_identity.outputs.principalId
    principalName: api_identity.outputs.principalName
  }
}

output env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID

output env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output api_identity_id string = api_identity.outputs.id

output api_identity_clientId string = api_identity.outputs.clientId

output azpg_connectionString string = azpg.outputs.connectionString

output env_volumes_sqlserver_0 string = env.outputs.volumes_sqlserver_0