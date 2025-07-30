@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource api2_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('api2_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = api2_identity.id

output clientId string = api2_identity.properties.clientId

output principalId string = api2_identity.properties.principalId

output principalName string = api2_identity.name

output name string = api2_identity.name