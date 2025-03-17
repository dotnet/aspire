targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@metadata({azd: {
  type: 'generate'
  config: {length:22,noSpecial:true}
  }
})
@secure()
param cache_password string
param certificateName string
param customDomain string
@secure()
param secretparam string

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module account 'account/account.module.bicep' = {
  name: 'account'
  scope: rg
  params: {
    keyVaultName: infra.outputs.secret_output_account
    location: location
  }
}
module api_roles 'api-roles/api-roles.module.bicep' = {
  name: 'api-roles'
  scope: rg
  params: {
    location: location
    storage_outputs_name: storage.outputs.name
  }
}
module infra 'infra/infra.module.bicep' = {
  name: 'infra'
  scope: rg
  params: {
    location: location
    principalId: ''
  }
}
module storage 'storage/storage.module.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
  }
}
output API_ROLES_CLIENTID string = api_roles.outputs.clientId
output API_ROLES_ID string = api_roles.outputs.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = infra.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output INFRA_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = infra.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output INFRA_AZURE_CONTAINER_REGISTRY_ENDPOINT string = infra.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output INFRA_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = infra.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output INFRA_SECRET_OUTPUT_ACCOUNT string = infra.outputs.secret_output_account
output INFRA_VOLUMES_CACHE_0 string = infra.outputs.volumes_cache_0
output STORAGE_BLOBENDPOINT string = storage.outputs.blobEndpoint
