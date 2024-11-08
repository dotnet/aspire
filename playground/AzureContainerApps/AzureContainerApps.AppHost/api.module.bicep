@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param storage_name string

param api_containerport string

param storage_outputs_blobendpoint string

param account_secretoutputs string

param outputs_azure_container_registry_managed_identity_id string

@secure()
param secretparam_value string

param outputs_azure_container_apps_environment_id string

param outputs_azure_container_registry_endpoint string

param api_containerimage string

param certificateName string

param customDomain string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storage_name
}

resource storage_a5cd3dcd 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.properties.principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    principalId: identity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
  }
  scope: storage
}

resource account_secretoutputs_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: account_secretoutputs
}

resource account_secretoutputs_kv_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
  name: 'connectionString'
  parent: account_secretoutputs_kv
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'api'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--account'
          identity: outputs_azure_container_registry_managed_identity_id
          keyVaultUrl: account_secretoutputs_kv_connectionString.properties.secretUri
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
            certificateId: (certificateName != '') ? '${outputs_azure_container_apps_environment_id}/managedCertificates/${certificateName}' : null
          }
        ]
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
              value: 'cache:6379'
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
              name: 'AZURE_CLIENT_ID'
              value: identity.properties.clientId
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
      '${identity.id}': { }
      '${outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}