param applicationType string = 'web'

param kind string = 'web'

param logAnalyticsWorkspaceId string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
    name: take('appInsights-${uniqueString(resourceGroup().id)}', 260)
    kind: kind
    location: resourceGroup().location
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