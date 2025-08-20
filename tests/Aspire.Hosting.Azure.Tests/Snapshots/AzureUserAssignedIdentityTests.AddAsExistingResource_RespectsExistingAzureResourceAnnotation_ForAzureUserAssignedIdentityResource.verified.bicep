@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_identity_name string

resource test_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: existing_identity_name
}

output id string = test_identity.id

output clientId string = test_identity.properties.clientId

output principalId string = test_identity.properties.principalId

output principalName string = existing_identity_name

output name string = existing_identity_name