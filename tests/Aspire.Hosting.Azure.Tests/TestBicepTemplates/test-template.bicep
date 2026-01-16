param location string = resourceGroup().location
param testParameter string

resource testResource 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: 'teststorage${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: {
    testParam: testParameter
  }
}

output storageEndpoint string = testResource.properties.primaryEndpoints.blob
