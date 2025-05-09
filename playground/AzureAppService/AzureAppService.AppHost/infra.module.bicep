@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param tags object = { }

resource infra_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('infra_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource infra_acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take('infraacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource infra_acr_infra_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(infra_acr.id, infra_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: infra_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: infra_acr
}

resource infra_asplan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: take('infraasplan-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    reserved: true
  }
  kind: 'Linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

output planId string = infra_asplan.id

output AZURE_CONTAINER_REGISTRY_NAME string = infra_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = infra_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = infra_mi.id

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = infra_mi.properties.clientId