﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_registry_endpoint string

param env_outputs_planid string

param env_outputs_azure_container_registry_managed_identity_id string

param env_outputs_azure_container_registry_managed_identity_client_id string

param project1_containerimage string

param project1_containerport string

param env_outputs_azure_app_service_dashboard_uri string

param env_outputs_azure_website_contributor_managed_identity_id string

param env_outputs_azure_website_contributor_managed_identity_principal_id string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-11-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: project1_containerimage
    isMain: true
    targetPort: project1_containerport
    userManagedIdentityClientId: env_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource webapp 'Microsoft.Web/sites@2024-11-01' = {
  name: take('${toLower('project1')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: env_outputs_planid
    siteConfig: {
      numberOfWorkers: 30
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
      appSettings: [
        {
          name: 'WEBSITES_PORT'
          value: project1_containerport
        }
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
          value: project1_containerport
        }
        {
          name: 'ASPIRE_ENVIRONMENT_NAME'
          value: 'env'
        }
        {
          name: 'OTEL_SERVICE_NAME'
          value: 'project1'
        }
        {
          name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
          value: 'grpc'
        }
        {
          name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
          value: 'http://localhost:6001'
        }
        {
          name: 'WEBSITE_ENABLE_ASPIRE_OTEL_SIDECAR'
          value: 'true'
        }
        {
          name: 'OTEL_COLLECTOR_URL'
          value: env_outputs_azure_app_service_dashboard_uri
        }
        {
          name: 'OTEL_CLIENT_ID'
          value: env_outputs_azure_container_registry_managed_identity_client_id
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

resource project1_ra 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(webapp.id, env_outputs_azure_website_contributor_managed_identity_id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'de139f84-1756-47ae-9be6-808fbbe84772'))
  properties: {
    principalId: env_outputs_azure_website_contributor_managed_identity_principal_id
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'de139f84-1756-47ae-9be6-808fbbe84772')
    principalType: 'ServicePrincipal'
  }
  scope: webapp
}