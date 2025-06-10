@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_azure_container_registry_endpoint string

param infra_outputs_planid string

param infra_outputs_azure_container_registry_managed_identity_id string

param infra_outputs_azure_container_registry_managed_identity_client_id string

param api_containerimage string

param api_containerport string

param storage_outputs_blobendpoint string

param account_outputs_connectionstring string

@secure()
param secretparam_value string

param api_identity_outputs_principalname string

param api_identity_outputs_id string

param api_identity_outputs_clientid string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-04-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: api_containerimage
    isMain: true
    userManagedIdentityClientId: infra_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource webapp 'Microsoft.Web/sites@2024-04-01' = {
  name: take('${toLower('api')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: infra_outputs_planid
    keyVaultReferenceIdentity: api_identity_outputs_id
    siteConfig: {
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: infra_outputs_azure_container_registry_managed_identity_client_id
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
          name: 'ConnectionStrings__account'
          value: account_outputs_connectionstring
        }
        {
          name: 'VALUE'
          value: secretparam_value
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
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${infra_outputs_azure_container_registry_managed_identity_id}': { }
      '${api_identity_outputs_id}': { }
    }
  }
}