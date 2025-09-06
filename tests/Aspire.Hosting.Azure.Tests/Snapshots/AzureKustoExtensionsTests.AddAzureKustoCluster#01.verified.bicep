@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param kusto_outputs_name string

param principalType string

param principalId string

resource kusto 'Microsoft.Kusto/clusters@2024-04-13' existing = {
  name: kusto_outputs_name
}

resource kusto_b24988ac_6180_42a0_ab88_20f7382dd24c 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kusto.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'Contributor'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'Contributor')
    principalType: principalType
  }
  scope: kusto
}