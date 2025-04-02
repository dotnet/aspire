@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

@secure()
param pg_password_value string

param env_outputs_azure_container_registry_managed_identity_id string

param env_outputs_azure_container_apps_environment_id string

resource pg 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'pg'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'postgres-password'
          value: pg_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5432
        transport: 'tcp'
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'docker.io/library/postgres:17.2'
          name: 'pg'
          env: [
            {
              name: 'POSTGRES_HOST_AUTH_METHOD'
              value: 'scram-sha-256'
            }
            {
              name: 'POSTGRES_INITDB_ARGS'
              value: '--auth-host=scram-sha-256 --auth-local=scram-sha-256'
            }
            {
              name: 'POSTGRES_USER'
              value: 'postgres'
            }
            {
              name: 'POSTGRES_PASSWORD'
              secretRef: 'postgres-password'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}