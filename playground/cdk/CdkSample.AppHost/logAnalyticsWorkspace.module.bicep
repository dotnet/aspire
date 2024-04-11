targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location


resource operationalInsightsWorkspace_DuWNVIPPL 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: toLower(take('logAnalyticsWorkspace${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'logAnalyticsWorkspace'
  }
  properties: {
    sku: {
      name: 'PerNode'
    }
  }
}

output logAnalyticsWorkspaceId string = operationalInsightsWorkspace_DuWNVIPPL.id
