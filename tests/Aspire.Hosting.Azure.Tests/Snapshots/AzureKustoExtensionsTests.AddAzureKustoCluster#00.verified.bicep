@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource kusto 'Microsoft.Kusto/clusters@2024-04-13' = {
  name: take('kusto${uniqueString(resourceGroup().id)}', 24)
  location: location
  sku: {
    name: 'Standard_D11_v2'
    capacity: 2
    tier: 'Standard'
  }
  tags: {
    'aspire-resource-name': 'kusto'
  }
}

output clusterUri string = kusto.name

output name string = kusto.name