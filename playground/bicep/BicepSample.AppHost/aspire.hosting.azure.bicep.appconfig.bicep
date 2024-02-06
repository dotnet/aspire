param configName string

param principalId string

param principalType string = 'ServicePrincipal'

param sku string = 'free'

param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01'= {
  name: replace('${configName}-${resourceToken}', '-', '')
  location: location
  sku: {
    name: sku
  }
}

resource AppConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfig.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
  scope: appConfig
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
  }
}

output appConfigEndpoint string = appConfig.properties.endpoint
