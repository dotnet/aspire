@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param tags object = { }

var resourceToken = uniqueString(resourceGroup().id)

resource env_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${resourceToken}'
  location: location
  tags: tags
}

resource env_acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('acr-${resourceToken}', '-', '')
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource env_acr_env_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(env_acr.id, env_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: env_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: env_acr
}

resource env_law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${resourceToken}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: env_law.properties.customerId
        sharedKey: env_law.listKeys().primarySharedKey
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
  parent: env
}

resource env_storageVolume 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: 'vol${resourceToken}'
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    largeFileSharesState: 'Enabled'
  }
  tags: tags
}

resource storageVolumeFileService 'Microsoft.Storage/storageAccounts/fileServices@2024-01-01' = {
  name: 'default'
  parent: env_storageVolume
}

resource shares_volumes_cache_0 'Microsoft.Storage/storageAccounts/fileServices/shares@2024-01-01' = {
  name: take('${toLower('cache')}-${toLower('Appdata')}', 60)
  properties: {
    enabledProtocols: 'SMB'
    shareQuota: 1024
  }
  parent: storageVolumeFileService
}

resource managedStorage_volumes_cache_0 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
  name: take('${toLower('cache')}-${toLower('Appdata')}', 32)
  properties: {
    azureFile: {
      accountName: env_storageVolume.name
      accountKey: env_storageVolume.listKeys().keys[0].value
      accessMode: 'ReadWrite'
      shareName: shares_volumes_cache_0.name
    }
  }
  parent: env
}

output volumes_cache_0 string = managedStorage_volumes_cache_0.name

output MANAGED_IDENTITY_NAME string = 'mi-${resourceToken}'

output MANAGED_IDENTITY_PRINCIPAL_ID string = env_mi.properties.principalId

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = 'law-${resourceToken}'

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = env_law.id

output AZURE_CONTAINER_REGISTRY_NAME string = replace('acr-${resourceToken}', '-', '')

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = env_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env_mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = 'cae-${resourceToken}'

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = env.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = env.properties.defaultDomain