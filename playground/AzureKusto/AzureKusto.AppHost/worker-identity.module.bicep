@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource worker_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('worker_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = worker_identity.id

output clientId string = worker_identity.properties.clientId

output principalId string = worker_identity.properties.principalId

output principalName string = worker_identity.name

output name string = worker_identity.name