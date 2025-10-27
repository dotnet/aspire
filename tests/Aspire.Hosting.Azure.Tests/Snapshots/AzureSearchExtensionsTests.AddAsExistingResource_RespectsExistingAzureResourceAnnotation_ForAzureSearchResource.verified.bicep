@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_search_name string

param existing_search_rg string

resource test_search 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: existing_search_name
  scope: resourceGroup(existing_search_rg)
}