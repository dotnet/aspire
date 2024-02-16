param appInsightsName string

param applicationType string = 'web'
param kind string = 'web'

param location string = resourceGroup().location
param logAnalyticsWorkspaceId string = ''

var resourceToken = uniqueString(resourceGroup().id)

@description('Tags that will be applied to all resources')
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = if (empty(logAnalyticsWorkspaceId)) {
  name: 'law-${appInsightsName}-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}


resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: '${appInsightsName}-${resourceToken}'
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: (empty(logAnalyticsWorkspaceId) ? logAnalyticsWorkspace.id : logAnalyticsWorkspaceId)
  }
  tags: tags
}

output appInsightsConnectionString string = appInsights.properties.ConnectionString
