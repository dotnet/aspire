@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param api_identity_outputs_id string

param api_identity_outputs_clientid string

param api_containerport string

param storage_outputs_blobendpoint string

@secure()
param cache_password_value string

param account_kv_outputs_name string

@secure()
param secretparam_value string

param api_identity_outputs_principalname string

param infra_outputs_azure_container_apps_environment_default_domain string

param infra_outputs_azure_container_apps_environment_id string

param infra_outputs_azure_container_registry_endpoint string

param infra_outputs_azure_container_registry_managed_identity_id string

param api_containerimage string

param certificateName string

param customDomain string

resource account_kv_outputs_name_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: account_kv_outputs_name
}

resource account_kv_outputs_name_kv_connectionstrings__account 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
  name: 'connectionstrings--account'
  parent: account_kv_outputs_name_kv
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'api'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--cache'
          value: 'cache:6379,password=${cache_password_value}'
        }
        {
          name: 'connectionstrings--account'
          identity: api_identity_outputs_id
          keyVaultUrl: account_kv_outputs_name_kv_connectionstrings__account.properties.secretUri
        }
        {
          name: 'value'
          value: secretparam_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: api_containerport
        transport: 'http'
        customDomains: [
          {
            name: customDomain
            bindingType: (certificateName != '') ? 'SniEnabled' : 'Disabled'
            certificateId: (certificateName != '') ? '${infra_outputs_azure_container_apps_environment_id}/managedCertificates/${certificateName}' : null
          }
        ]
      }
      registries: [
        {
          server: infra_outputs_azure_container_registry_endpoint
          identity: infra_outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: infra_outputs_azure_container_apps_environment_id
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
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: api_containerport
            }
            {
              name: 'ConnectionStrings__blobs'
              value: storage_outputs_blobendpoint
            }
            {
              name: 'ConnectionStrings__cache'
              secretRef: 'connectionstrings--cache'
            }
            {
              name: 'ConnectionStrings__account'
              secretRef: 'connectionstrings--account'
            }
            {
              name: 'VALUE'
              secretRef: 'value'
            }
            {
              name: 'AZURE_PRINCIPAL_NAME'
              value: api_identity_outputs_principalname
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: api_identity_outputs_clientid
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${api_identity_outputs_id}': { }
      '${infra_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}