@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param outputs_azure_container_registry_managed_identity_id string

param outputs_azure_container_apps_environment_id string

param outputs_azure_container_registry_endpoint string

param pythonapp_containerimage string

resource pythonapp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'pythonapp'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: outputs_azure_container_registry_endpoint
          identity: outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: pythonapp_containerimage
          name: 'pythonapp'
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
      '${outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}