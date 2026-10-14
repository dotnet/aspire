targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

@secure()
param secret string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module generated 'generated/generated.bicep' = {
  name: 'generated'
  scope: rg
  params: {
    location: location
    enabled: false
    count: 2
    redirectUris: [
      'https://localhost:7123/signin-oidc'
    ]
    options: json('{"enabled":true}')
    jsonArray: json('["one",2]')
    jsonBoolean: json('true')
    jsonInteger: json('3')
    secret: secret
  }
}