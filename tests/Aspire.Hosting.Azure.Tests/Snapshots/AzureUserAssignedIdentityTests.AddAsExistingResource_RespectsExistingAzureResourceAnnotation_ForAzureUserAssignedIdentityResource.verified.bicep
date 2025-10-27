@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_identity_name string

param existing_identity_rg string

resource test_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: existing_identity_name
  scope: resourceGroup(existing_identity_rg)
}