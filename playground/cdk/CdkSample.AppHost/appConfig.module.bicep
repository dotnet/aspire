targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource appConfigurationStore_xM7mBhesj 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: toLower(take('appConfig${uniqueString(resourceGroup().id)}', 24))
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

resource roleAssignment_3uatMWw7h 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: appConfigurationStore_xM7mBhesj
  name: guid(appConfigurationStore_xM7mBhesj.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
    principalId: principalId
    principalType: principalType
  }
}

output appConfigEndpoint string = appConfigurationStore_xM7mBhesj.properties.endpoint
