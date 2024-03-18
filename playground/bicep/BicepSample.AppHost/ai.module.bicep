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


resource applicationInsightsComponent_qG5w9sTHc 'Microsoft.Insights/components@2020-02-02' = {
  name: toLower(take(concat('ai', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'ai'
  }
  kind: kind
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output appInsightsConnectionString string = applicationInsightsComponent_qG5w9sTHc.properties.ConnectionString
