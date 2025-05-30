﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_name string

resource env 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: env_outputs_azure_container_apps_environment_name
}

output id string = env.id
