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

resource infra_law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: take('infralaw-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource infra 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: take('infra${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: infra_law.properties.customerId
        sharedKey: infra_law.listKeys().primarySharedKey
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
  parent: infra
}

resource infra_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(infra.id, userPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
  properties: {
    principalId: userPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
  }
  scope: infra
}

resource infra_storageVolume 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('infrastoragevolume${uniqueString(resourceGroup().id)}', 24)
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
  parent: infra_storageVolume
}

resource shares_volumes_cache_0 'Microsoft.Storage/storageAccounts/fileServices/shares@2024-01-01' = {
  name: take('sharesvolumescache0-${uniqueString(resourceGroup().id)}', 63)
  properties: {
    enabledProtocols: 'SMB'
    shareQuota: 1024
  }
  parent: storageVolumeFileService
}

resource managedStorage_volumes_cache_0 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
  name: take('managedstoragevolumescache${uniqueString(resourceGroup().id)}', 24)
  properties: {
    azureFile: {
      accountName: infra_storageVolume.name
      accountKey: infra_storageVolume.listKeys().keys[0].value
      accessMode: 'ReadWrite'
      shareName: shares_volumes_cache_0.name
    }
  }
  parent: infra
}

output volumes_cache_0 string = managedStorage_volumes_cache_0.name

output MANAGED_IDENTITY_NAME string = infra_mi.name

output MANAGED_IDENTITY_PRINCIPAL_ID string = infra_mi.properties.principalId

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = infra_law.name

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = infra_law.id

output AZURE_CONTAINER_REGISTRY_NAME string = infra_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = infra_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = infra_mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = infra.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = infra.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = infra.properties.defaultDomain