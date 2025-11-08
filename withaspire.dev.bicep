targetScope = 'resourceGroup'

@description('The name of the Static Web App')
param staticWebAppName string = 'withaspire-dev'

@description('The location for the Static Web App')
param location string = resourceGroup().location

@description('The SKU name for the Static Web App')
@allowed([
  'Free'
  'Standard'
])
param skuName string = 'Free'

@description('The repository URL for the Static Web App')
param repositoryUrl string = 'https://github.com/dotnet/aspire'

@description('The repository branch for the Static Web App')
param repositoryBranch string = 'main'

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: repositoryBranch
    buildProperties: {
      appLocation: '/withaspire.dev'
      apiLocation: ''
      outputLocation: ''
    }
  }
}

output staticWebAppId string = staticWebApp.id
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppName string = staticWebApp.name
