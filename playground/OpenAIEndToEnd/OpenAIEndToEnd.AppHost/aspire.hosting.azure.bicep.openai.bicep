// c.f., https://learn.microsoft.com/azure/ai-services/create-account-bicep

param name string
param principalId string
param principalType string = 'ServicePrincipal'
param deployments array = [] // This is a placeholder. Deployments provisioning is not supported yet.

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Location for all resources.')
param location string = resourceGroup().location

@allowed([
  'S0'
])
param sku string = 'S0'

var resourceToken = uniqueString(resourceGroup().id)

resource cognitiveService 'Microsoft.CognitiveServices/accounts@2021-10-01' = {
  name: '${name}-${resourceToken}'
  location: location
  sku: {
    name: sku
  }
  kind: 'OpenAI'
  properties: {
    apiProperties: {
      statisticsEnabled: false
    }
  }
  tags: tags
}

// Find list of roles and GUIDs in https://learn.microsoft.com/azure/role-based-access-control/built-in-roles

// Cognitive Services OpenAI Contributor
var contributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
resource cognitiveServiceContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(cognitiveService.id, principalId, contributorRole)
  scope: cognitiveService
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: contributorRole
  }
}

output connectionString string = 'Endpoint=${cognitiveService.properties.endpoint}'
