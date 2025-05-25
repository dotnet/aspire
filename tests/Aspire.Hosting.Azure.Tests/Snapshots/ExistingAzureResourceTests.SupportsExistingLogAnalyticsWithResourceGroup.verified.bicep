@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: existingResourceName
}

output logAnalyticsWorkspaceId string = logAnalytics.id
output name string = logAnalytics.name