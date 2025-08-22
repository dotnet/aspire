@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_appconfig_name string

param existing_appconfig_rg string

resource test_app_config 'Microsoft.AppConfiguration/configurationStores@2024-06-01' existing = {
  name: existing_appconfig_name
  scope: resourceGroup(existing_appconfig_rg)
}