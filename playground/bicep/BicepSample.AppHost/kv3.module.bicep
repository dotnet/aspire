targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalType string


resource keyVault_AlZz71Qpf 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: toLower(take('kv3${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'kv3'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
}

resource roleAssignment_B2rItKEaQ 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault_AlZz71Qpf
  name: guid(keyVault_AlZz71Qpf.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: principalId
    principalType: principalType
  }
}

output vaultUri string = keyVault_AlZz71Qpf.properties.vaultUri
