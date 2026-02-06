@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param nat_outputs_id string

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

resource aca_nsg 'Microsoft.Network/networkSecurityGroups@2025-05-01' = {
  name: take('aca_nsg-${uniqueString(resourceGroup().id)}', 80)
  location: location
}

resource aca_nsg_allow_https_from_azure_lb 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'allow-https-from-azure-lb'
  properties: {
    access: 'Allow'
    destinationAddressPrefix: '*'
    destinationPortRange: '443'
    direction: 'Inbound'
    priority: 100
    protocol: 'Tcp'
    sourceAddressPrefix: 'AzureLoadBalancer'
    sourcePortRange: '*'
  }
  parent: aca_nsg
}

resource aca_nsg_deny_vnet_inbound 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'deny-vnet-inbound'
  properties: {
    access: 'Deny'
    destinationAddressPrefix: '*'
    destinationPortRange: '*'
    direction: 'Inbound'
    priority: 110
    protocol: '*'
    sourceAddressPrefix: 'VirtualNetwork'
    sourcePortRange: '*'
  }
  parent: aca_nsg
}

resource aca_nsg_deny_internet_inbound 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'deny-internet-inbound'
  properties: {
    access: 'Deny'
    destinationAddressPrefix: '*'
    destinationPortRange: '*'
    direction: 'Inbound'
    priority: 4096
    protocol: '*'
    sourceAddressPrefix: 'Internet'
    sourcePortRange: '*'
  }
  parent: aca_nsg
}

resource pe_nsg 'Microsoft.Network/networkSecurityGroups@2025-05-01' = {
  name: take('pe_nsg-${uniqueString(resourceGroup().id)}', 80)
  location: location
}

resource pe_nsg_allow_https_from_vnet 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'allow-https-from-vnet'
  properties: {
    access: 'Allow'
    destinationAddressPrefix: '*'
    destinationPortRange: '443'
    direction: 'Inbound'
    priority: 100
    protocol: 'Tcp'
    sourceAddressPrefix: 'VirtualNetwork'
    sourcePortRange: '*'
  }
  parent: pe_nsg
}

resource pe_nsg_deny_all_internet_inbound 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'deny-all-internet-inbound'
  properties: {
    access: 'Deny'
    destinationAddressPrefix: '*'
    destinationPortRange: '*'
    direction: 'Inbound'
    priority: 4096
    protocol: '*'
    sourceAddressPrefix: 'Internet'
    sourcePortRange: '*'
  }
  parent: pe_nsg
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
        name: 'Microsoft.App/environments'
      }
    ]
    natGateway: {
      id: nat_outputs_id
    }
    networkSecurityGroup: {
      id: aca_nsg.id
    }
  }
  parent: vnet
}

resource private_endpoints 'Microsoft.Network/virtualNetworks/subnets@2025-05-01' = {
  name: 'private-endpoints'
  properties: {
    addressPrefix: '10.0.2.0/27'
    networkSecurityGroup: {
      id: pe_nsg.id
    }
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