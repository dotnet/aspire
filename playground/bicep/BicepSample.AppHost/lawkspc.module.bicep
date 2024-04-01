targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location


resource operationalInsightsWorkspace_cxL77xv9Y 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: toLower(take(concat('lawkspc', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'lawkspc'
  }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

output logAnalyticsWorkspaceId string = operationalInsightsWorkspace_cxL77xv9Y.id
