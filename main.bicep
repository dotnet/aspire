targetScope = 'resourceGroup'

@description('The location for all resources')
param location string = resourceGroup().location

@description('The repository URL for the Static Web Apps')
param repositoryUrl string = 'https://github.com/dotnet/aspire'

@description('The repository branch for the Static Web Apps')
param repositoryBranch string = 'main'

@description('The SKU name for the Static Web Apps')
@allowed([
  'Free'
  'Standard'
])
param skuName string = 'Free'

module withaspiredev 'withaspire.dev.bicep' = {
  name: 'withaspire-dev-deployment'
  params: {
    location: location
    repositoryUrl: repositoryUrl
    repositoryBranch: repositoryBranch
    skuName: skuName
  }
}

output withaspiredev_staticWebAppId string = withaspiredev.outputs.staticWebAppId
output withaspiredev_staticWebAppDefaultHostname string = withaspiredev.outputs.staticWebAppDefaultHostname
output withaspiredev_staticWebAppName string = withaspiredev.outputs.staticWebAppName
