@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

resource ehstorage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
    name: toLower(take('ehstorage${uniqueString(resourceGroup().id)}', 24))
    kind: 'StorageV2'
    location: location
    sku: {
        name: 'Standard_GRS'
    }
    properties: {
        accessTier: 'Hot'
        allowSharedKeyAccess: false
        minimumTlsVersion: 'TLS1_2'
        networkAcls: {
            defaultAction: 'Allow'
        }
    }
    tags: {
        'aspire-resource-name': 'ehstorage'
    }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
    name: 'default'
    parent: ehstorage
}

resource ehstorage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(ehstorage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
        principalType: principalType
    }
    scope: ehstorage
}

resource ehstorage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(ehstorage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
        principalType: principalType
    }
    scope: ehstorage
}

resource ehstorage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(ehstorage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
        principalType: principalType
    }
    scope: ehstorage
}

output blobEndpoint string = ehstorage.properties.primaryEndpoints.blob

output queueEndpoint string = ehstorage.properties.primaryEndpoints.queue

output tableEndpoint string = ehstorage.properties.primaryEndpoints.table