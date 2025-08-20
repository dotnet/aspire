@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_openai_name string

resource test_openai 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: existing_openai_name
}

output connectionString string = 'Endpoint=${test_openai.properties.endpoint}'

output name string = existing_openai_name