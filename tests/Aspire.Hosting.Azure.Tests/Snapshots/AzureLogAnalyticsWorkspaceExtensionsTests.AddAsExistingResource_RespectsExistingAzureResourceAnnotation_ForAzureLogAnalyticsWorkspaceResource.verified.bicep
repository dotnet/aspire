@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_logworkspace_name string

param existing_logworkspace_rg string

resource test_log_workspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: existing_logworkspace_name
  scope: resourceGroup(existing_logworkspace_rg)
}