@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_redis_name string

resource test_redis 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: existing_redis_name
}

output connectionString string = '${test_redis.properties.hostName},ssl=true'

output name string = existing_redis_name