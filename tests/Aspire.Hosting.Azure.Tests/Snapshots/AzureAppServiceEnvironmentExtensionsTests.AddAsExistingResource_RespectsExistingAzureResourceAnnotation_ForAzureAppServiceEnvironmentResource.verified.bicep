@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param tags object = { }

resource test_app_service_env_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('test_app_service_env_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource test_app_service_env_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: take('testappserviceenvacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource test_app_service_env_acr_test_app_service_env_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(test_app_service_env_acr.id, test_app_service_env_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: test_app_service_env_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: test_app_service_env_acr
}

resource test_app_service_env_asplan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: take('testappserviceenvasplan-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    reserved: true
  }
  kind: 'Linux'
  sku: {
    name: 'P0V3'
    tier: 'Premium'
  }
}

output name string = test_app_service_env_asplan.name

output planId string = test_app_service_env_asplan.id

output AZURE_CONTAINER_REGISTRY_NAME string = test_app_service_env_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = test_app_service_env_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = test_app_service_env_mi.id

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = test_app_service_env_mi.properties.clientId