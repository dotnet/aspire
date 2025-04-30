@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param tags object = { }

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take('acr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource acr_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: acr
}

resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: take('law-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource cae 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: take('cae${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
  tags: tags
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
  parent: cae
}

resource cae_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(cae.id, userPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
  properties: {
    principalId: userPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
  }
  scope: cae
}

output MANAGED_IDENTITY_NAME string = mi.name

output MANAGED_IDENTITY_PRINCIPAL_ID string = mi.properties.principalId

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = law.name

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = law.id

output AZURE_CONTAINER_REGISTRY_NAME string = acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = cae.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = cae.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = cae.properties.defaultDomain