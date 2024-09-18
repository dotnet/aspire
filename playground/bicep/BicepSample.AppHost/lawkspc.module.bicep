@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource lawkspc 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
    name: toLower(take('lawkspc${uniqueString(resourceGroup().id)}', 24))
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