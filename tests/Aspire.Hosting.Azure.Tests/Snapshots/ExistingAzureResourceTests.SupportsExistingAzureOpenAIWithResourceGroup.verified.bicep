﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingResourceName string

resource openAI 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: existingResourceName
}

resource mymodel 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'mymodel'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
  }
  sku: {
    name: 'Basic'
    capacity: 4
  }
  parent: openAI
}

output connectionString string = 'Endpoint=${openAI.properties.endpoint}'

output name string = existingResourceName