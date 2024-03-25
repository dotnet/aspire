param location string
param tags object = {}
                                  
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
    name: 'cai-${uniqueString(resourceGroup().id)}'
    location: location
    tags: tags
}
                                                                                                                   
output id string = identity.id
output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
output name string = identity.name