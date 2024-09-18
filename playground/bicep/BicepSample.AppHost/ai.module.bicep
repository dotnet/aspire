@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param logAnalyticsWorkspaceId string

resource ai 'Microsoft.Insights/components@2020-02-02' = {
    name: toLower(take('ai${uniqueString(resourceGroup().id)}', 24))
    kind: kind
    location: location
    properties: {
        Application_Type: applicationType
        WorkspaceResourceId: logAnalyticsWorkspaceId
    }
    tags: {
        'aspire-resource-name': 'ai'
    }
}

output appInsightsConnectionString string = ai.properties.ConnectionString