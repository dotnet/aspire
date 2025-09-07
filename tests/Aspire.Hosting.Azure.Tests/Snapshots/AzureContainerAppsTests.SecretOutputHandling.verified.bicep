@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param outputs_azure_container_apps_environment_default_domain string

param outputs_azure_container_apps_environment_id string

param api_identity_outputs_id string

param mydb_kv_outputs_name string

param mydb_secretoutputs string

@secure()
param mydb_secretoutputs_connectionstring string

@secure()
param mydb_secretoutputs_connectionstring1 string

param api_identity_outputs_clientid string

resource mydb_kv_outputs_name_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: mydb_kv_outputs_name
}

resource mydb_kv_outputs_name_kv_connectionstrings__mydb 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
  name: 'connectionstrings--mydb'
  parent: mydb_kv_outputs_name_kv
}

resource mydb_secretoutputs_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: mydb_secretoutputs
}

resource mydb_secretoutputs_kv_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
  name: 'connectionString'
  parent: mydb_secretoutputs_kv
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'api'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--mydb'
          identity: api_identity_outputs_id
          keyVaultUrl: mydb_kv_outputs_name_kv_connectionstrings__mydb.properties.secretUri
        }
        {
          name: 'connectionstring'
          identity: api_identity_outputs_id
          keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
        }
        {
          name: 'secret0'
          identity: api_identity_outputs_id
          keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
        }
        {
          name: 'secret1'
          identity: api_identity_outputs_id
          keyVaultUrl: mydb_secretoutputs_kv_connectionString.properties.secretUri
        }
        {
          name: 'complex'
          value: 'a/${mydb_secretoutputs_connectionstring}/${mydb_secretoutputs_connectionstring}/${mydb_secretoutputs_connectionstring1}'
        }
      ]
      activeRevisionsMode: 'Single'
    }
    environmentId: outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'image:latest'
          name: 'api'
          env: [
            {
              name: 'ConnectionStrings__mydb'
              secretRef: 'connectionstrings--mydb'
            }
            {
              name: 'connectionString'
              secretRef: 'connectionstring'
            }
            {
              name: 'secret0'
              secretRef: 'secret0'
            }
            {
              name: 'secret1'
              secretRef: 'secret1'
            }
            {
              name: 'complex'
              secretRef: 'complex'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: api_identity_outputs_clientid
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${api_identity_outputs_id}': { }
    }
  }
}