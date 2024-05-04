param location string
param tags object = {}
param sku string = 'Basic'
param adminUserEnabled bool = true

var resourceToken = uniqueString(resourceGroup().id)
        
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
    name: replace('acr${resourceToken}', '-', '')
    location: location
    sku: {
        name: sku
    }
    properties: {
        adminUserEnabled: adminUserEnabled
    }
    tags: tags
}
        
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
    name: 'mi-${resourceToken}'
    location: location
    tags: tags
}

resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
    scope: containerRegistry
    properties: {
        principalId: managedIdentity.properties.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    }
}

output mid string = managedIdentity.id
output loginServer string = containerRegistry.properties.loginServer