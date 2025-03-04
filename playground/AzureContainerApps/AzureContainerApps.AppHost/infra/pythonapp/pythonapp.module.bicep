@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param resources_outputs_azure_container_registry_managed_identity_id string

param resources_outputs_managed_identity_client_id string

param resources_outputs_azure_container_apps_environment_id string

param resources_outputs_azure_container_registry_endpoint string

param pythonapp_containerimage string

resource pythonapp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'pythonapp'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: resources_outputs_azure_container_registry_endpoint
          identity: resources_outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: resources_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: pythonapp_containerimage
          name: 'pythonapp'
          env: [
            {
              name: 'AZURE_CLIENT_ID'
              value: resources_outputs_managed_identity_client_id
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
      '${resources_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}