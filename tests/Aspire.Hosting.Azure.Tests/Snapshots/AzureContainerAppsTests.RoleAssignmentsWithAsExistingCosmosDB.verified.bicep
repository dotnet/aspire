﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param api_identity_outputs_id string

param api_identity_outputs_clientid string

param cosmos_outputs_connectionstring string

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

param env_outputs_azure_container_registry_endpoint string

param env_outputs_azure_container_registry_managed_identity_id string

param api_containerimage string

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'api'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: env_outputs_azure_container_registry_endpoint
          identity: env_outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: api_containerimage
          name: 'api'
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
              name: 'ConnectionStrings__cosmos'
              value: cosmos_outputs_connectionstring
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: api_identity_outputs_clientid
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
      '${api_identity_outputs_id}': { }
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}