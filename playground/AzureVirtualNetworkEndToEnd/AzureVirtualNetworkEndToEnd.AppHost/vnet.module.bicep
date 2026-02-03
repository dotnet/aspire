@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource vnet 'Microsoft.Network/virtualNetworks@2025-05-01' = {
  name: take('vnet-${uniqueString(resourceGroup().id)}', 64)
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
  }
  location: location
  tags: {
    'aspire-resource-name': 'vnet'
  }
}

resource container_apps 'Microsoft.Network/virtualNetworks/subnets@2025-05-01' = {
  name: 'container-apps'
  properties: {
    addressPrefix: '10.0.0.0/23'
    delegations: [
      {
        properties: {
          serviceName: 'Microsoft.App/environments'
        }
        name: 'ContainerAppsDelegation'
      }
    ]
  }
  parent: vnet
}

resource private_endpoints 'Microsoft.Network/virtualNetworks/subnets@2025-05-01' = {
  name: 'private-endpoints'
  properties: {
    addressPrefix: '10.0.2.0/27'
  }
  parent: vnet
  dependsOn: [
    container_apps
  ]
}

output container_apps_Id string = container_apps.id

output private_endpoints_Id string = private_endpoints.id

output id string = vnet.id

output name string = vnet.name