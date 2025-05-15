﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take('acr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: {
    'aspire-resource-name': 'acr'
  }
}

output name string = acr.name

output loginServer string = acr.properties.loginServer