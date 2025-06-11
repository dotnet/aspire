@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource funcapp_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('funcapp_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = funcapp_identity.id

output clientId string = funcapp_identity.properties.clientId

output principalId string = funcapp_identity.properties.principalId

output principalName string = funcapp_identity.name

output name string = funcapp_identity.name