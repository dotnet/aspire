targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource appConfigurationStore_j2IqAZkBh 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: toLower(take(concat('appConfig', uniqueString(resourceGroup().id)), 24))
  location: location
  sku: {
    name: 'free'
  }
  properties: {
  }
}

output appConfigurationStore_j2IqAZkBh_endpoint string = appConfigurationStore_j2IqAZkBh.properties.endpoint
