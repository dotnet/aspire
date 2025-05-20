@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param appservice_outputs_azure_container_registry_endpoint string

param appservice_outputs_planid string

param appservice_outputs_azure_container_registry_managed_identity_id string

param appservice_outputs_azure_container_registry_managed_identity_client_id string

param myapp_containerimage string

param myidentity_outputs_id string

param myidentity_outputs_clientid string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-04-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: myapp_containerimage
    isMain: true
    userManagedIdentityClientId: appservice_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource webapp 'Microsoft.Web/sites@2024-04-01' = {
  name: take('${toLower('myapp')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: appservice_outputs_planid
    keyVaultReferenceIdentity: myidentity_outputs_id
    siteConfig: {
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: appservice_outputs_azure_container_registry_managed_identity_client_id
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
          name: 'AZURE_CLIENT_ID'
          value: myidentity_outputs_clientid
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${appservice_outputs_azure_container_registry_managed_identity_id}': { }
      '${myidentity_outputs_id}': { }
    }
  }
}