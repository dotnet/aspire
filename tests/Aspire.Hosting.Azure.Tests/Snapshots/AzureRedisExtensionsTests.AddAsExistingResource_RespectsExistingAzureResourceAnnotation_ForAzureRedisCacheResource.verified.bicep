﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_redis_name string

param existing_redis_rg string

resource test_redis 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: existing_redis_name
  scope: resourceGroup(existing_redis_rg)
}