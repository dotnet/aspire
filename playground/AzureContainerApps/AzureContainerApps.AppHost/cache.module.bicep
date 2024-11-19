@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param cache_volumes_0_storage string

param outputs_azure_container_registry_managed_identity_id string

param outputs_azure_container_apps_environment_id string

resource cache 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'cache'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    environmentId: outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'docker.io/library/redis:7.4'
          name: 'cache'
          args: [
            '--save'
            '60'
            '1'
          ]
          volumeMounts: [
            {
              volumeName: 'v0'
              mountPath: '/data'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
      volumes: [
        {
          name: 'v0'
          storageType: 'AzureFile'
          storageName: cache_volumes_0_storage
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}