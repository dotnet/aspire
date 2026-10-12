targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module registry 'registry/registry.bicep' = {
  name: 'registry'
  scope: resourceGroup('registry-rg')
  params: {
    location: location
  }
}

module aks 'aks/aks.bicep' = {
  name: 'aks'
  scope: rg
  params: {
    location: location
  }
}

module aks_acr_pull 'aks-acr-pull/aks-acr-pull.bicep' = {
  name: 'aks-acr-pull'
  scope: resourceGroup('registry-rg')
  params: {
    location: location
    acrName: registry.outputs.name
    aksId: aks.outputs.id
    principalId: aks.outputs.kubeletIdentityObjectId
  }
}