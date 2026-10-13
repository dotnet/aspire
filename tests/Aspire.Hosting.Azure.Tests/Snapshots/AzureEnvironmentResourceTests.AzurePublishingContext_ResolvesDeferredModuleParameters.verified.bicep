targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

param suffix string = 'web'

@secure()
param secret string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module source 'source/source.bicep' = {
  name: 'source'
  scope: rg
  params: {
    location: location
  }
}

module mod 'mod/mod.bicep' = {
  name: 'mod'
  scope: rg
  params: {
    location: location
    displayName: 'Contoso\'s web application'
    redirectUris: [
      'https://localhost:7123/signin-oidc'
      'https://localhost:7252/callback'
    ]
    options: json('{"scopes":["access_as_user"],"enabled":true}')
    enabled: false
    count: 2
    identifier: '00001111-aaaa-2222-bbbb-3333cccc4444'
    suffix: suffix
    secret: secret
    clientId: source.outputs.clientId
    combined: '${suffix}-${source.outputs.clientId}'
    optional: ''
  }
}