targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module acaEnv 'acaEnv/acaEnv.bicep' = {
  name: 'acaEnv'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}

module included_storage 'included-storage/included-storage.bicep' = {
  name: 'included-storage'
  scope: rg
  params: {
    location: location
  }
}