@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource lawkspc 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: take('lawkspc-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'aspire-resource-name': 'lawkspc'
  }
}

output logAnalyticsWorkspaceId string = lawkspc.id

output name string = lawkspc.name