param managedIdentityName string

@description('Tags that will be applied to all resources')
param tags object = {}

param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${managedIdentityName}-${resourceToken}'
  location: location
  tags: tags
}

output principalId string = managedIdentity.properties.principalId
output clientId string = managedIdentity.properties.clientId
