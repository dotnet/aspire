@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
  name: take('appConfig-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: 'standard'
  }
  tags: {
    'aspire-resource-name': 'appConfig'
  }
}

output appConfigEndpoint string = appConfig.properties.endpoint

output name string = appConfig.name