param principalId string

param principalType string

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2019-10-01' = {
    name: take('appConfig-${uniqueString(resourceGroup().id)}', 50)
    location: resourceGroup().location
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

resource AppConfigurationDataOwner_appConfig 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(resourceGroup().id, 'AppConfigurationDataOwner_appConfig')
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
        principalType: principalType
    }
    scope: appConfig
}

output appConfigEndpoint string = appConfig.properties.endpoint