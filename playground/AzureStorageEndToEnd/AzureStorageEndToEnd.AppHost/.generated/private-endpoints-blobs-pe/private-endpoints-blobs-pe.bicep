@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param vnet_outputs_private_endpoints_id string

param storage_outputs_id string

resource private_endpoints_blobs_pe 'Microsoft.Network/privateEndpoints@2025-05-01' = {
  name: take('private_endpoints_blobs_pe-${uniqueString(resourceGroup().id)}', 64)
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        properties: {
          privateLinkServiceId: storage_outputs_id
          groupIds: [
            'blob'
          ]
        }
        name: 'private-endpoints-blobs-pe-connection'
      }
    ]
    subnet: {
      id: vnet_outputs_private_endpoints_id
    }
  }
  tags: {
    'aspire-resource-name': 'private-endpoints-blobs-pe'
  }
}

output id string = private_endpoints_blobs_pe.id

output name string = private_endpoints_blobs_pe.name