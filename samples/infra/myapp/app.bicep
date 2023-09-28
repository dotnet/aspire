param location string = resourceGroup().location
param acrPullRoleDefinitionName string = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
param acrPushRoleDefinitionName string = '8311e382-0749-4cb8-b61a-304f252e45ec'
param contributorRoleDefinitionName string = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
@secure()
param postgresServerPassword string = newGuid()
resource storemydata2srv 'Microsoft.DBforPostgreSQL/flexibleServers@2021-06-01' = {
  name: 'pgsql${uniqueString(resourceGroup().id, 'storemydata2')}'
  location: location
  sku: {
    name: 'Standard_D4ds_v4'
    tier: 'GeneralPurpose'
  }
  properties: {
    version: '12'
    administratorLogin: 'postgres'
    administratorLoginPassword: postgresServerPassword
    storage: {
      storageSizeGB: 128
    }
  }
}
resource cachymccacheface2 'Microsoft.Cache/redis@2022-06-01' = {
  name: 'redis${uniqueString(resourceGroup().id, 'cachymccacheface2')}'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
  }
}
resource logWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: 'logs${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: 'env${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logWorkspace.properties.customerId
        sharedKey: logWorkspace.listKeys().primarySharedKey
      }
    }
  }
}
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2022-02-01-preview' = {
  name: 'registry${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Basic'
  }
}
resource acrPushIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'id${uniqueString(resourceGroup().id, 'push')}'
  location: location
}
resource acrPullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'id${uniqueString(resourceGroup().id, 'pull')}'
  location: location
}
resource contributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: contributorRoleDefinitionName
  scope: subscription()
}
resource acrPullRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: acrPullRoleDefinitionName
  scope: subscription()
}
resource contributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrPushIdentity.id, contributorRoleDefinitionName, containerRegistry.id)
  properties: {
    principalId: acrPushIdentity.properties.principalId
    roleDefinitionId: contributorRoleDefinition.id
    principalType: 'ServicePrincipal'
  }
  scope: containerRegistry
}
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrPullIdentity.id, acrPullRoleDefinitionName, containerRegistry.id)
  properties: {
    principalId: acrPullIdentity.properties.principalId
    roleDefinitionId: acrPullRoleDefinition.id
    principalType: 'ServicePrincipal'
  }
  scope: containerRegistry
}
resource containerImageBootstrapScript0 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'import${uniqueString(resourceGroup().id, 'containerImageBootstrapScript0')}'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${acrPushIdentity.id}': {
      }
    }
  }
  properties: {
    azCliVersion: '2.47.0'
    retentionInterval: 'P1D'
    scriptContent: 'az acr import --name ${containerRegistry.name} --source mcr.microsoft.com/azuredocs/containerapps-helloworld:latest --resource-group ${resourceGroup().name} --image catalog:latest'
  }
}
resource containerApp0 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'catalog-${uniqueString(resourceGroup().id)}'
  location: location
  dependsOn: [
containerImageBootstrapScript0  ]
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {
      }
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      registries: [
        {
          identity: acrPullIdentity.id
          server: containerRegistry.properties.loginServer
        }
      ]
    }
    template: {
      scale: {
        minReplicas: 2
      }
      containers: [
        {
          name: 'container${uniqueString(resourceGroup().id)}'
          image: '${containerRegistry.properties.loginServer}/catalog:latest'
          env: [
            {
              name: 'ConnectionStrings__Aspire.PostgreSQL'
              value: 'Host=localhost;Database=catalog;Username=postgres;Password=postgres'
            }
          ]
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
resource containerImageBootstrapScript1 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'import${uniqueString(resourceGroup().id, 'containerImageBootstrapScript1')}'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${acrPushIdentity.id}': {
      }
    }
  }
  properties: {
    azCliVersion: '2.47.0'
    retentionInterval: 'P1D'
    scriptContent: 'az acr import --name ${containerRegistry.name} --source mcr.microsoft.com/azuredocs/containerapps-helloworld:latest --resource-group ${resourceGroup().name} --image basket:latest'
  }
}
resource containerApp1 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'basket-${uniqueString(resourceGroup().id)}'
  location: location
  dependsOn: [
containerImageBootstrapScript1  ]
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {
      }
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      registries: [
        {
          identity: acrPullIdentity.id
          server: containerRegistry.properties.loginServer
        }
      ]
    }
    template: {
      scale: {
        minReplicas: 2
      }
      containers: [
        {
          name: 'container${uniqueString(resourceGroup().id)}'
          image: '${containerRegistry.properties.loginServer}/basket:latest'
          env: [
          ]
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
resource containerImageBootstrapScript2 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'import${uniqueString(resourceGroup().id, 'containerImageBootstrapScript2')}'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${acrPushIdentity.id}': {
      }
    }
  }
  properties: {
    azCliVersion: '2.47.0'
    retentionInterval: 'P1D'
    scriptContent: 'az acr import --name ${containerRegistry.name} --source mcr.microsoft.com/azuredocs/containerapps-helloworld:latest --resource-group ${resourceGroup().name} --image myfrontend:latest'
  }
}
resource containerApp2 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'myfrontend-${uniqueString(resourceGroup().id)}'
  location: location
  dependsOn: [
containerImageBootstrapScript2  ]
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {
      }
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      registries: [
        {
          identity: acrPullIdentity.id
          server: containerRegistry.properties.loginServer
        }
      ]
    }
    template: {
      scale: {
        minReplicas: 2
      }
      containers: [
        {
          name: 'container${uniqueString(resourceGroup().id)}'
          image: '${containerRegistry.properties.loginServer}/myfrontend:latest'
          env: [
          ]
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
resource containerImageBootstrapScript3 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'import${uniqueString(resourceGroup().id, 'containerImageBootstrapScript3')}'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${acrPushIdentity.id}': {
      }
    }
  }
  properties: {
    azCliVersion: '2.47.0'
    retentionInterval: 'P1D'
    scriptContent: 'az acr import --name ${containerRegistry.name} --source mcr.microsoft.com/azuredocs/containerapps-helloworld:latest --resource-group ${resourceGroup().name} --image orderprocessor:latest'
  }
}
resource containerApp3 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'orderprocessor-${uniqueString(resourceGroup().id)}'
  location: location
  dependsOn: [
containerImageBootstrapScript3  ]
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {
      }
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      registries: [
        {
          identity: acrPullIdentity.id
          server: containerRegistry.properties.loginServer
        }
      ]
    }
    template: {
      scale: {
        minReplicas: 2
      }
      containers: [
        {
          name: 'container${uniqueString(resourceGroup().id)}'
          image: '${containerRegistry.properties.loginServer}/orderprocessor:latest'
          env: [
          ]
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
resource containerImageBootstrapScript4 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'import${uniqueString(resourceGroup().id, 'containerImageBootstrapScript4')}'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${acrPushIdentity.id}': {
      }
    }
  }
  properties: {
    azCliVersion: '2.47.0'
    retentionInterval: 'P1D'
    scriptContent: 'az acr import --name ${containerRegistry.name} --source mcr.microsoft.com/azuredocs/containerapps-helloworld:latest --resource-group ${resourceGroup().name} --image apigateway:latest'
  }
}
resource containerApp4 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'apigateway-${uniqueString(resourceGroup().id)}'
  location: location
  dependsOn: [
containerImageBootstrapScript4  ]
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${acrPullIdentity.id}': {
      }
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      registries: [
        {
          identity: acrPullIdentity.id
          server: containerRegistry.properties.loginServer
        }
      ]
    }
    template: {
      scale: {
        minReplicas: 2
      }
      containers: [
        {
          name: 'container${uniqueString(resourceGroup().id)}'
          image: '${containerRegistry.properties.loginServer}/apigateway:latest'
          env: [
          ]
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
