@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param config_outputs_name string

param principalId string

resource config 'Microsoft.AppConfiguration/configurationStores@2024-06-01' existing = {
  name: config_outputs_name
}

resource config_AppConfigurationDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(config.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071')
    principalType: 'ServicePrincipal'
  }
  scope: config
}