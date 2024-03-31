targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param applicationType string = 'web'

@description('')
param kind string = 'web'

@description('')
param logAnalyticsWorkspaceId string

@description('')
param keyVaultName string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource applicationInsightsComponent_qG5w9sTHc 'Microsoft.Insights/components@2020-02-02' = {
  name: toLower(take(concat('ai', uniqueString(resourceGroup().id)), 24))
  location: location
  tags: {
    'aspire-resource-name': 'ai'
  }
  kind: kind
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: '${keyVaultSecret_Ddsc3HjrA.ConnectionString}'
  }
}
