param principalId string

param principalType string

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
    name: take('search-${uniqueString(resourceGroup().id)}', 60)
    location: resourceGroup().location
    properties: {
        hostingMode: 'default'
        disableLocalAuth: true
        partitionCount: 1
        replicaCount: 1
    }
    sku: {
        name: 'basic'
    }
    tags: {
        'aspire-resource-name': 'search'
    }
}

resource SearchIndexDataContributor_search 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(resourceGroup().id, 'SearchIndexDataContributor_search')
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
        principalType: principalType
    }
    scope: search
}

resource SearchServiceContributor_search 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(resourceGroup().id, 'SearchServiceContributor_search')
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
        principalType: principalType
    }
    scope: search
}

output connectionString string = 'Endpoint=https://${search.name}.search.windows.net'