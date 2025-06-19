﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource myidentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('myidentity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = myidentity.id

output clientId string = myidentity.properties.clientId

output principalId string = myidentity.properties.principalId

output principalName string = myidentity.name

output name string = myidentity.name