targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param applicationType string = 'web'

@description('')
param kind string = 'web'

@description('')
param logAnalyticsWorkspaceId string

@description('')
param principalId string

@description('')
param principalType string


resource applicationInsightsComponent_fo9MneV12 'Microsoft.Insights/components@2020-02-02' = {
  name: toLower(take(concat('appInsights', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'appInsights'
  }
  kind: kind
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
  }
}

output appInsightsConnectionString string = applicationInsightsComponent_fo9MneV12.properties.ConnectionString
