@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_appenv_name string

param existing_appenv_rg string

resource test_app_service_env 'Microsoft.Web/serverfarms@2024-11-01' existing = {
  name: existing_appenv_name
  scope: resourceGroup(existing_appenv_rg)
}