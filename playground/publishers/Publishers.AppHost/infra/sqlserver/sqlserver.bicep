@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_volumes_sqlserver_0 string

@secure()
param sqlserver_password_value string

param env_outputs_azure_container_registry_managed_identity_id string

param env_outputs_azure_container_apps_environment_id string

resource sqlserver 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'sqlserver'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'mssql-sa-password'
          value: sqlserver_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 1433
        transport: 'tcp'
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'mcr.microsoft.com/mssql/server:2022-latest'
          name: 'sqlserver'
          env: [
            {
              name: 'ACCEPT_EULA'
              value: 'Y'
            }
            {
              name: 'MSSQL_SA_PASSWORD'
              secretRef: 'mssql-sa-password'
            }
          ]
          volumeMounts: [
            {
              volumeName: 'v0'
              mountPath: '/var/opt/mssql'
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
          storageName: env_outputs_volumes_sqlserver_0
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}