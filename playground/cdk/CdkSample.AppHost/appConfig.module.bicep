@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2019-10-01' = {
    name: toLower(take('appConfig${uniqueString(resourceGroup().id)}', 24))
    location: location
    properties: {
        disableLocalAuth: true
    }
    sku: {
        name: 'standard'
    }
    tags: {
        'aspire-resource-name': 'appConfig'
    }
}

resource appConfig_AppConfigurationDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(appConfig.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
        principalType: principalType
    }
    scope: appConfig
}

output appConfigEndpoint string = appConfig.properties.endpoint