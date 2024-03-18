targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param sku string


resource appConfigurationStore_j2IqAZkBh 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: toLower(take(concat('appConfig', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'appConfig'
  }
  sku: {
    name: 'standard'
  }
  properties: {
  }
}

resource roleAssignment_umUNaNdeG 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: appConfigurationStore_j2IqAZkBh
  name: guid(appConfigurationStore_j2IqAZkBh.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
    principalId: principalId
    principalType: principalType
  }
}

output appConfigEndpoint string = appConfigurationStore_j2IqAZkBh.properties.endpoint
