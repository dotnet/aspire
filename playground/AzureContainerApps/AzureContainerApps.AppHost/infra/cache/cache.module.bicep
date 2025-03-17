@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_volumes_cache_0 string

@secure()
param cache_password_value string

param infra_outputs_azure_container_registry_managed_identity_id string

param infra_outputs_azure_container_apps_environment_id string

resource cache 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'cache'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'redis-password'
          value: cache_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    environmentId: infra_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'docker.io/library/redis:7.4'
          name: 'cache'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'redis-server --requirepass \$REDIS_PASSWORD --save 60 1'
          ]
          env: [
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-password'
            }
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
          storageName: infra_outputs_volumes_cache_0
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${infra_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}