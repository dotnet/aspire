﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_sb_name string

resource test_servicebus 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: existing_sb_name
}

output serviceBusEndpoint string = test_servicebus.properties.serviceBusEndpoint

output name string = existing_sb_name