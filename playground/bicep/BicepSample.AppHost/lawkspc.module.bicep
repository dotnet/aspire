targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location


resource operationalInsightsWorkspace_FFogvqZja 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: toLower(take('lawkspc${uniqueString(resourceGroup().id)}', 24))
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

output logAnalyticsWorkspaceId string = operationalInsightsWorkspace_FFogvqZja.id
