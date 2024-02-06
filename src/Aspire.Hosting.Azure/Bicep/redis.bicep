@description('Specify the name of the Azure Redis Cache to create.')
param redisCacheName string

@description('Location of all resources')
param location string = resourceGroup().location

@description('Specify the pricing tier of the new Azure Redis Cache.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Basic'

@description('Specify the family for the sku. C = Basic/Standard, P = Premium.')
@allowed([
  'C'
  'P'
])
param family string = 'C'

@description('Specify the size of the new Azure Redis Cache instance. Valid values: for C (Basic/Standard) family (0, 1, 2, 3, 4, 5, 6), for P (Premium) family (1, 2, 3, 4)')
@allowed([
  0
  1
  2
  3
  4
  5
  6
])
param capacity int = 1

var resourceToken = uniqueString(resourceGroup().id)

resource redisCache 'Microsoft.Cache/Redis@2020-06-01' = {
  name: '${redisCacheName}-${resourceToken}'
  location: location
  properties: {
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    sku: {
      capacity: capacity
      family: family
      name: sku
    }
  }
}

output cacheName string = redisCache.name
output hostName string = redisCache.properties.hostName
