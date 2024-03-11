targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string

@description('')
param keyVaultName string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

resource cognitiveServicesAccount_DqMZSfXbZ 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

resource roleAssignment_biQFQxikh 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: cognitiveServicesAccount_DqMZSfXbZ
  name: guid(cognitiveServicesAccount_DqMZSfXbZ.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
    principalId: principalId
    principalType: principalType
  }
}

resource cognitiveServicesAccountDeployment_0OtUTY1oh 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: cognitiveServicesAccount_DqMZSfXbZ
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

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: 'Endpoint=${cognitiveServicesAccount_DqMZSfXbZ.properties.endpoint}'
  }
}
