@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param kusto_outputs_name string

param principalType string

param principalId string

resource kusto 'Microsoft.Kusto/clusters@2024-04-13' existing = {
  name: kusto_outputs_name
}

resource kusto_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kusto.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalType: principalType
  }
  scope: kusto
}