targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource cognitiveServicesAccount_wXAGTFUId 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: toLower(take('openai${uniqueString(resourceGroup().id)}', 24))
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower(take(concat('openai', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource roleAssignment_Hsk8rxWY8 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: cognitiveServicesAccount_wXAGTFUId
  name: guid(cognitiveServicesAccount_wXAGTFUId.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
    principalId: principalId
    principalType: principalType
  }
}

resource cognitiveServicesAccountDeployment_oFQ0tEiLR 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: cognitiveServicesAccount_wXAGTFUId
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

output connectionString string = 'Endpoint=${cognitiveServicesAccount_wXAGTFUId.properties.endpoint}'
