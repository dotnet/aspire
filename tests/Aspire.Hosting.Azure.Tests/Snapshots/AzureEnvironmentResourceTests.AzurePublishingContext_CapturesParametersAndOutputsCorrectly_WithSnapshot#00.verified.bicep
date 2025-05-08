targetScope = 'subscription'

param azure_rg_default string

param azure_location_default string

param principalId string

param storage_Sku string = 'Standard_LRS'

param skuDescription string = 'The sku is '

var tags = {
  'aspire-env-name': azure_rg_default
}

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: azure_rg_default
  location: azure_location_default
  tags: tags
}

module acaEnv 'acaEnv/acaEnv.bicep' = {
  name: 'acaEnv'
  scope: rg
  params: {
    location: azure_location_default
    userPrincipalId: principalId
  }
}

module kv 'kv/kv.bicep' = {
  name: 'kv'
  scope: rg
  params: {
    location: azure_location_default
  }
}

module account 'account/account.bicep' = {
  name: 'account'
  scope: rg
  params: {
    location: azure_location_default
  }
}

module storage 'storage/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: azure_location_default
    storage_Sku: storage_Sku
    sku_description: '${skuDescription} ${storage_Sku}'
  }
}

module fe_identity 'fe-identity/fe-identity.bicep' = {
  name: 'fe-identity'
  scope: rg
  params: {
    location: azure_location_default
  }
}

module fe_roles_storage 'fe-roles-storage/fe-roles-storage.bicep' = {
  name: 'fe-roles-storage'
  scope: rg
  params: {
    location: azure_location_default
    storage_outputs_name: storage.outputs.name
    principalId: fe_identity.outputs.principalId
  }
}

module fe_roles_account 'fe-roles-account/fe-roles-account.bicep' = {
  name: 'fe-roles-account'
  scope: rg
  params: {
    location: azure_location_default
    account_outputs_name: account.outputs.name
    principalId: fe_identity.outputs.principalId
  }
}

output acaEnv_AZURE_CONTAINER_REGISTRY_NAME string = acaEnv.outputs.AZURE_CONTAINER_REGISTRY_NAME

output acaEnv_AZURE_CONTAINER_REGISTRY_ENDPOINT string = acaEnv.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output acaEnv_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = acaEnv.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output acaEnv_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = acaEnv.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output acaEnv_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = acaEnv.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID

output fe_identity_id string = fe_identity.outputs.id

output storage_blobEndpoint string = storage.outputs.blobEndpoint

output account_connectionString string = account.outputs.connectionString

output fe_identity_clientId string = fe_identity.outputs.clientId

output kv_vaultUri string = kv.outputs.vaultUri