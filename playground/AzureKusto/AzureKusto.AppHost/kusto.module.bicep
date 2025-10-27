@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource kusto 'Microsoft.Kusto/clusters@2024-04-13' = {
  name: take('kusto${uniqueString(resourceGroup().id)}', 24)
  location: location
  sku: {
    name: 'Standard_E2a_v4'
    capacity: 2
    tier: 'Standard'
  }
  tags: {
    'aspire-resource-name': 'kusto'
  }
}

resource testdb 'Microsoft.Kusto/clusters/databases@2024-04-13' = {
  name: 'testdb'
  location: location
  parent: kusto
  kind: 'ReadWrite'
}

output clusterUri string = kusto.properties.uri

output name string = kusto.name