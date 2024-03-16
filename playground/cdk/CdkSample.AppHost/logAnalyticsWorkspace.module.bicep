targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource operationalInsightsWorkspace_uzGUFQdnZ 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: toLower(take(concat('logAnalyticsWorkspace', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'logAnalyticsWorkspace'
  }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 10
  }
}

output logAnalyticsWorkspaceId string = operationalInsightsWorkspace_uzGUFQdnZ.id
