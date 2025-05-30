@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_registry_endpoint string

param env_outputs_planid string

param env_outputs_azure_container_registry_managed_identity_id string

param env_outputs_azure_container_registry_managed_identity_client_id string

param api_containerimage string

param mydb_kv_outputs_name string

param api_identity_outputs_id string

param api_identity_outputs_clientid string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-11-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: api_containerimage
    isMain: true
    userManagedIdentityClientId: env_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource mydb_kv_outputs_name_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: mydb_kv_outputs_name
}

resource mydb_kv_outputs_name_kv_connectionstrings__mydb 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
  name: 'connectionstrings--mydb'
  parent: mydb_kv_outputs_name_kv
}

resource webapp 'Microsoft.Web/sites@2024-11-01' = {
  name: take('${toLower('api')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: env_outputs_planid
    keyVaultReferenceIdentity: api_identity_outputs_id
    siteConfig: {
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
      appSettings: [
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
          name: 'ConnectionStrings__mydb'
          value: '@Microsoft.KeyVault(SecretUri=${mydb_kv_outputs_name_kv_connectionstrings__mydb.properties.secretUri})'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: api_identity_outputs_clientid
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
      '${api_identity_outputs_id}': { }
    }
  }
}