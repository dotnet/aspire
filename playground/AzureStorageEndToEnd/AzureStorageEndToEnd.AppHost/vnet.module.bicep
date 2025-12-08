@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource vnet 'Microsoft.Network/virtualNetworks@2025-01-01' = {
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

resource subnet1 'Microsoft.Network/virtualNetworks/subnets@2025-01-01' = {
  name: 'subnet1'
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

output subnet1_Id string = subnet1.id

output id string = vnet.id

output name string = vnet.name