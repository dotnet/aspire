@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: take('logAnalyticsWorkspace-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerNode'
    }
  }
  tags: {
    'aspire-resource-name': 'logAnalyticsWorkspace'
  }
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id