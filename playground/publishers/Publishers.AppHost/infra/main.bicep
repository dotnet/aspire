targetScope = 'subscription'

param environmentName string

param location string

param principalId string

var tags = {
  'aspire-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module azpg 'azpg/azpg.bicep' = {
  name: 'azpg'
  scope: rg
  params: {
    location: location
    principalId: ''
    principalType: ''
    principalName: ''
  }
}

output azpg_connectionString string = azpg.outputs.connectionString