targetScope = 'resourceGroup'

@description('')
param logAnalyticsWorkspaceId string = ''

@description('')
param location string = resourceGroup().location

@description('')
param applicationType string = 'web'

@description('')
param kind string = 'web'

@description('')
param principalId string

@description('')
param principalType string


resource operationalInsightsWorkspace_fo9MneV12 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: toLower(take(concat('appInsights', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'appInsights'
  }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsightsComponent_fo9MneV12 'Microsoft.Insights/components@2020-02-02' = {
  name: toLower(take(concat('appInsights', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'appInsights'
  }
  kind: kind
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: (empty(logAnalyticsWorkspaceId) ? operationalInsightsWorkspace_fo9MneV12.id : logAnalyticsWorkspaceId)
  }
}

output appInsightsConnectionString string = applicationInsightsComponent_fo9MneV12.properties.ConnectionString
