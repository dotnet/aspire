targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module aca_env 'aca-env/aca-env.bicep' = {
  name: 'aca-env'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}

module azure_storage 'azure-storage/azure-storage.bicep' = {
  name: 'azure-storage'
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

module api_roles_azure_storage 'api-roles-azure-storage/api-roles-azure-storage.bicep' = {
  name: 'api-roles-azure-storage'
  scope: rg
  params: {
    location: location
    azure_storage_outputs_name: azure_storage.outputs.name
    principalId: api_identity.outputs.principalId
  }
}

output aca_env_AZURE_CONTAINER_REGISTRY_NAME string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_NAME

output aca_env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output aca_env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output aca_env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output aca_env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID

output api_identity_id string = api_identity.outputs.id

output azure_storage_dataLakeEndpoint string = azure_storage.outputs.dataLakeEndpoint

output api_identity_clientId string = api_identity.outputs.clientId