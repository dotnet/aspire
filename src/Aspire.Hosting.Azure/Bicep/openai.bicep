// c.f., https://learn.microsoft.com/azure/ai-services/create-account-bicep

param name string
param principalId string
param principalType string = 'ServicePrincipal'
param deployments array = []

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Location for all resources.')
param location string = resourceGroup().location

@allowed([ 'Enabled', 'Disabled' ])
param publicNetworkAccess string = 'Enabled'

@allowed([
  'S0'
])
param sku string = 'S0'

param allowedIpRules array = []
param networkAcls object = empty(allowedIpRules) ? {
  defaultAction: 'Allow'
} : {
  ipRules: allowedIpRules
  defaultAction: 'Deny'
}

var resourceToken = uniqueString(resourceGroup().id)
var accountName = '${name}-${resourceToken}'

resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: accountName
  location: location
  sku: {
    name: sku
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: publicNetworkAccess
    networkAcls: networkAcls
  }
  tags: tags
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: {
  parent: account
  name: deployment.name
  properties: {
    model: deployment.model
  }
  sku: deployment.sku
}]

// Find list of roles and GUIDs in https://learn.microsoft.com/azure/role-based-access-control/built-in-roles

// Cognitive Services OpenAI Contributor
var contributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
resource cognitiveServiceContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(account.id, principalId, contributorRole)
  scope: account
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: contributorRole
  }
}

output connectionString string = 'Endpoint=${account.properties.endpoint}'
