@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource cache 'Microsoft.Cache/redis@2024-03-01' = {
  name: take('cache-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
    enableNonSslPort: false
    disableAccessKeyAuthentication: true
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'aad-enabled': 'true'
    }
  }
  tags: {
    'aspire-resource-name': 'cache'
  }
}

output connectionString string = '${cache.properties.hostName},ssl=true'

output name string = cache.name