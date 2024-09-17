@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param logAnalyticsWorkspaceId string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
    name: toLower(take('appInsights${uniqueString(resourceGroup().id)}', 24))
    kind: kind
    location: location
    properties: {
        Application_Type: applicationType
        IngestionMode: 'LogAnalytics'
        WorkspaceResourceId: logAnalyticsWorkspaceId
    }
    tags: {
        'aspire-resource-name': 'appInsights'
    }
}

output appInsightsConnectionString string = appInsights.properties.ConnectionString