@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_logworkspace_name string

resource test_log_workspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: existing_logworkspace_name
}

output logAnalyticsWorkspaceId string = test_log_workspace.id

output name string = existing_logworkspace_name