@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param apiservice_containerport string

param eventhubs_outputs_eventhubsendpoint string

param storage_outputs_queueendpoint string

param storage_outputs_blobendpoint string

param outputs_azure_container_apps_environment_default_domain string

param outputs_azure_container_registry_managed_identity_id string

param outputs_managed_identity_client_id string

param outputs_azure_container_apps_environment_id string

param outputs_azure_container_registry_endpoint string

param apiservice_containerimage string

resource apiservice 'Microsoft.App/containerApps@2024-10-02-preview' = {
  name: 'apiservice'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: apiservice_containerport
        transport: 'http'
      }
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
          image: apiservice_containerimage
          name: 'apiservice'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: apiservice_containerport
            }
            {
              name: 'ConnectionStrings__myhub'
              value: 'Endpoint=${eventhubs_outputs_eventhubsendpoint};EntityPath=myhub'
            }
            {
              name: 'ConnectionStrings__queue'
              value: storage_outputs_queueendpoint
            }
            {
              name: 'ConnectionStrings__blob'
              value: storage_outputs_blobendpoint
            }
            {
              name: 'services__funcapp__http__0'
              value: 'http://funcapp.${outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__funcapp__https__0'
              value: 'https://funcapp.${outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: outputs_managed_identity_client_id
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
      '${outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}