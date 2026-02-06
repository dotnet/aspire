@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource myvnet 'Microsoft.Network/virtualNetworks@2025-05-01' = {
  name: take('myvnet-${uniqueString(resourceGroup().id)}', 64)
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
  }
  location: location
  tags: {
    'aspire-resource-name': 'myvnet'
  }
}

resource web_nsg 'Microsoft.Network/networkSecurityGroups@2025-05-01' = {
  name: take('web_nsg-${uniqueString(resourceGroup().id)}', 80)
  location: location
}

output id string = myvnet.id

output name string = myvnet.name