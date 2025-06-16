@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource apiservice_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('apiservice_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = apiservice_identity.id

output clientId string = apiservice_identity.properties.clientId

output principalId string = apiservice_identity.properties.principalId

output principalName string = apiservice_identity.name

output name string = apiservice_identity.name