@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param vnet_outputs_id string

param vnet_outputs_private_endpoints_id string

param storage_outputs_id string

resource privatelink_queue_core_windows_net 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.queue.core.windows.net'
  location: 'global'
  tags: {
    'aspire-resource-name': 'private-endpoints-queues-pe-dns'
  }
}

resource privatelink_queue_core_windows_net_vnetlink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  name: 'private-endpoints-queues-pe-vnet-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet_outputs_id
    }
  }
  tags: {
    'aspire-resource-name': 'private-endpoints-queues-pe-vnetlink'
  }
  parent: privatelink_queue_core_windows_net
}

resource private_endpoints_queues_pe 'Microsoft.Network/privateEndpoints@2025-05-01' = {
  name: take('private_endpoints_queues_pe-${uniqueString(resourceGroup().id)}', 64)
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        properties: {
          privateLinkServiceId: storage_outputs_id
          groupIds: [
            'queue'
          ]
        }
        name: 'private-endpoints-queues-pe-connection'
      }
    ]
    subnet: {
      id: vnet_outputs_private_endpoints_id
    }
  }
  tags: {
    'aspire-resource-name': 'private-endpoints-queues-pe'
  }
}

resource private_endpoints_queues_pe_dnsgroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2025-05-01' = {
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink_queue_core_windows_net'
        properties: {
          privateDnsZoneId: privatelink_queue_core_windows_net.id
        }
      }
    ]
  }
  parent: private_endpoints_queues_pe
}

output id string = private_endpoints_queues_pe.id

output name string = private_endpoints_queues_pe.name