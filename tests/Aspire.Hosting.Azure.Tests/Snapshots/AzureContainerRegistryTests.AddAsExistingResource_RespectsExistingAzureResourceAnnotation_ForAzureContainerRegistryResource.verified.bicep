@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_acr_name string

resource test_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' existing = {
  name: existing_acr_name
}

output name string = existing_acr_name

output loginServer string = test_acr.properties.loginServer