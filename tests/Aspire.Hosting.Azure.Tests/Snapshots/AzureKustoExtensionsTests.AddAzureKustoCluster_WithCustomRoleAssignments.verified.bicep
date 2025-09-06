@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param kusto_outputs_name string

param principalId string

resource kusto 'Microsoft.Kusto/clusters@2024-04-13' existing = {
  name: kusto_outputs_name
}

resource kusto_acdd72a7_3385_48ef_bd42_f606fba81ae7 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kusto.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'Reader'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'Reader')
    principalType: 'ServicePrincipal'
  }
  scope: kusto
}