@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource api1_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('api1_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = api1_identity.id

output clientId string = api1_identity.properties.clientId

output principalId string = api1_identity.properties.principalId

output principalName string = api1_identity.name

output name string = api1_identity.name