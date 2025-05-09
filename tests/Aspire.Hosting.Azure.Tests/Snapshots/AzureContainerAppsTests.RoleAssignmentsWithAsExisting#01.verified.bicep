﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param storage_outputs_name string

param principalId string

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storage_outputs_name
}

resource storage_StorageBlobDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
    principalType: 'ServicePrincipal'
  }
  scope: storage
}