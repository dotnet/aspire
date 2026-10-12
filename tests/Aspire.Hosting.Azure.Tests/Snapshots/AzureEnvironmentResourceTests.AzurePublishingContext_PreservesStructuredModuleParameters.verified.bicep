targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module mod 'mod/mod.bicep' = {
  name: 'mod'
  scope: rg
  params: {
    location: location
    redirectUris: [
      'https://localhost:7123/signin-oidc'
      'https://localhost:7252/callback'
    ]
    options: json('{"scopes":["access_as_user"],"enabled":true}')
    enabled: false
    count: 2
    identifier: '00001111-aaaa-2222-bbbb-3333cccc4444'
  }
}