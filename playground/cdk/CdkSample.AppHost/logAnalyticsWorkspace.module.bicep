targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location


resource operationalInsightsWorkspace_uzGUFQdnZ 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: toLower(take(concat('logAnalyticsWorkspace', uniqueString(resourceGroup().id)), 24))
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

output logAnalyticsWorkspaceId string = operationalInsightsWorkspace_uzGUFQdnZ.id
