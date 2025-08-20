@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_search_name string

resource test_search 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: existing_search_name
}

output connectionString string = 'Endpoint=https://${existing_search_name}.search.windows.net'

output name string = existing_search_name