targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module vnet 'vnet/vnet.bicep' = {
  name: 'vnet'
  scope: rg
  params: {
    location: location
  }
}

module env_acr 'env-acr/env-acr.bicep' = {
  name: 'env-acr'
  scope: rg
  params: {
    location: location
  }
}

module env 'env/env.bicep' = {
  name: 'env'
  scope: rg
  params: {
    location: location
    env_acr_outputs_name: env_acr.outputs.name
    vnet_outputs_subnet1_id: vnet.outputs.subnet1_Id
    userPrincipalId: principalId
  }
}

module storage 'storage/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
  }
}

module private_endpoints_blobs_pe 'private-endpoints-blobs-pe/private-endpoints-blobs-pe.bicep' = {
  name: 'private-endpoints-blobs-pe'
  scope: rg
  params: {
    location: location
    vnet_outputs_private_endpoints_id: vnet.outputs.private_endpoints_Id
    storage_outputs_id: storage.outputs.id
  }
}

module api_identity 'api-identity/api-identity.bicep' = {
  name: 'api-identity'
  scope: rg
  params: {
    location: location
  }
}

module api_roles_storage 'api-roles-storage/api-roles-storage.bicep' = {
  name: 'api-roles-storage'
  scope: rg
  params: {
    location: location
    storage_outputs_name: storage.outputs.name
    principalId: api_identity.outputs.principalId
  }
}

output env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID

output env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output api_identity_id string = api_identity.outputs.id

output storage_blobEndpoint string = storage.outputs.blobEndpoint

output storage_queueEndpoint string = storage.outputs.queueEndpoint

output api_identity_clientId string = api_identity.outputs.clientId