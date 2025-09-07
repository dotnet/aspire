﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

param api_identity_outputs_id string

param mydb_kv_outputs_name string

param kvName string

param sharedRg string

param api_identity_outputs_clientid string

resource mydb_kv 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: mydb_kv_outputs_name
}

resource mydb_kv_connectionstrings__mydb 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'connectionstrings--mydb'
  parent: mydb_kv
}

resource existingKv 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: kvName
  scope: resourceGroup(sharedRg)
}

resource existingKv_secret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'secret'
  parent: existingKv
}

resource api 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'api'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--mydb'
          identity: api_identity_outputs_id
          keyVaultUrl: mydb_kv_connectionstrings__mydb.properties.secretUri
        }
        {
          name: 'secret-value'
          identity: api_identity_outputs_id
          keyVaultUrl: existingKv_secret.properties.secretUri
        }
      ]
      activeRevisionsMode: 'Single'
    }
    environmentId: env_outputs_azure_container_apps_environment_id
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
              name: 'SECRET_VALUE'
              secretRef: 'secret-value'
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
