targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param storagesku string

@description('')
param locationOverride string

@description('')
param principalId string

@description('')
param principalType string


resource storageAccount_Tn41yfrBn 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: toLower(take(concat('cdkstorage2', uniqueString(resourceGroup().id)), 24))
  location: locationOverride
  sku: {
    name: storagesku
  }
  kind: 'Storage'
  properties: {
  }
}

resource blobService_L4VtbX3sf 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount_Tn41yfrBn
  name: 'default'
  properties: {
  }
}

resource roleAssignment_UtfaPuAd8 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_Tn41yfrBn
  name: guid(storageAccount_Tn41yfrBn.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_sw5JL5brt 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_Tn41yfrBn
  name: guid(storageAccount_Tn41yfrBn.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: principalId
    principalType: principalType
  }
}

resource roleAssignment_iCksqTYss 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount_Tn41yfrBn
  name: guid(storageAccount_Tn41yfrBn.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: principalId
    principalType: principalType
  }
}

output blobEndpoint string = storageAccount_Tn41yfrBn.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount_Tn41yfrBn.properties.primaryEndpoints.queue
output tableEndpoint string = storageAccount_Tn41yfrBn.properties.primaryEndpoints.table
