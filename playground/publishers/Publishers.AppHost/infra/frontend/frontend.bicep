@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param frontend_containerport string

@secure()
param sqlserver_password_value string

param param0_value string

@secure()
param param1_value string

param param2_value string

param param3_value string

param outputs_azure_container_apps_environment_default_domain string

param outputs_azure_container_registry_managed_identity_id string

param outputs_azure_container_apps_environment_id string

param outputs_azure_container_registry_endpoint string

param frontend_containerimage string

resource frontend 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'frontend'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--sqldb'
          value: '${'Server=sqlserver,1433;User ID=sa;Password=${sqlserver_password_value};TrustServerCertificate=true'};Database=sqldb'
        }
        {
          name: 'p1'
          value: param1_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: frontend_containerport
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
          image: frontend_containerimage
          name: 'frontend'
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
              value: frontend_containerport
            }
            {
              name: 'ConnectionStrings__sqldb'
              secretRef: 'connectionstrings--sqldb'
            }
            {
              name: 'P0'
              value: param0_value
            }
            {
              name: 'P1'
              secretRef: 'p1'
            }
            {
              name: 'P2'
              value: param2_value
            }
            {
              name: 'P3'
              value: param3_value
            }
            {
              name: 'services__api__http__0'
              value: 'http://api.${outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__api__https__0'
              value: 'https://api.${outputs_azure_container_apps_environment_default_domain}'
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