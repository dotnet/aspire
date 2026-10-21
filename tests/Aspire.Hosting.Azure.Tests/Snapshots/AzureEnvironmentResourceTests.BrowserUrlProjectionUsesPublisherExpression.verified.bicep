targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

param public_host string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module registration 'registration/registration.bicep' = {
  name: 'registration'
  scope: rg
  params: {
    location: location
    browserOrigin: 'https://${public_host}'
  }
}