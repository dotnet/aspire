@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource aks 'Microsoft.ContainerService/managedClusters@2026-01-01' existing = {
  name: 'existing-cluster'
  scope: resourceGroup('existing-subscription', 'existing-rg')
}