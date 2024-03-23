targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource cognitiveServicesAccount_6g8jyEjX5 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
  }
}

resource roleAssignment_X7ie0XqR2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: cognitiveServicesAccount_6g8jyEjX5
  name: guid(cognitiveServicesAccount_6g8jyEjX5.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
    principalId: principalId
    principalType: principalType
  }
}

resource cognitiveServicesAccountDeployment_f9rYX6SRK 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: cognitiveServicesAccount_6g8jyEjX5
  name: 'gpt-35-turbo'
  sku: {
    name: 'Standard'
    capacity: 1
  }
  properties: {
    model: {
      name: 'gpt-35-turbo'
      format: 'OpenAI'
      version: '0613'
    }
  }
}

output connectionString string = 'Endpoint=${cognitiveServicesAccount_6g8jyEjX5.properties.endpoint}'
output modelName string = cognitiveServicesAccountDeployment_f9rYX6SRK.name
