param location string
param tags object = {}
param parameters object = {}
param inputs object = {}

module test 'test.bicep' = {
    name: 'test'
    params: {
        location: location
        test: parameters.val
        p2: test0.outputs.val0
        values: ['one', 'two']
    }
}

module test0 'test0.module.bicep' = {
    name: 'test0'
    params: {
        location: location
    }
}

module kv3 'kv3.module.bicep' = {
    name: 'kv3'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalType: 'ServicePrincipal'
    }
}

module appConfig 'appConfig.module.bicep' = {
    name: 'appConfig'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalType: 'ServicePrincipal'
        sku: 'standard'
    }
}

module storage 'storage.module.bicep' = {
    name: 'storage'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalType: 'ServicePrincipal'
    }
}

module sql 'sql.module.bicep' = {
    name: 'sql'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalName: default_identity.outputs.name
        principalType: 'ServicePrincipal'
    }
}

module postgres2 'postgres2.module.bicep' = {
    name: 'postgres2'
    params: {
        location: location
        keyVaultName: postgres2_kv.name
        administratorLogin: parameters.administratorLogin
        administratorLoginPassword: parameters.administratorLoginPassword
    }
}

module cosmos 'cosmos.module.bicep' = {
    name: 'cosmos'
    params: {
        location: location
        keyVaultName: cosmos_kv.name
    }
}

module lawkspc 'lawkspc.module.bicep' = {
    name: 'lawkspc'
    params: {
        location: location
    }
}

module ai 'ai.module.bicep' = {
    name: 'ai'
    params: {
        location: location
        logAnalyticsWorkspaceId: lawkspc.outputs.logAnalyticsWorkspaceId
    }
}

module aiwithoutlaw 'aiwithoutlaw.module.bicep' = {
    name: 'aiwithoutlaw'
    params: {
        location: location
        logAnalyticsWorkspaceId: containerAppEnv.outputs.logAnalyticsWorkspaceId
    }
}

module redis 'redis.module.bicep' = {
    name: 'redis'
    params: {
        location: location
        keyVaultName: redis_kv.name
    }
}

module sb 'sb.module.bicep' = {
    name: 'sb'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalType: 'ServicePrincipal'
    }
}

module signalr 'signalr.module.bicep' = {
    name: 'signalr'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalType: 'ServicePrincipal'
    }
}

module containerAppEnv 'containerappenv.module.bicep' = {
    name: 'containerAppEnv'
    params: {
        location: location
    }
}

module containerRegistry 'containerregistry.module.bicep' = {
    name: 'containerRegistry'
    params: {
        location: location
    }
}

module default_identity 'default-identity.module.bicep' = {
    name: 'default-identity'
    params: {
        location: location
    }
}

module api_containerApp 'api-containerapp.module.bicep' = {
    name: 'api-containerApp'
    params: {
        location: location
        sql_outputs_sqlServerFqdn: sql.outputs.sqlServerFqdn
        postgres2_secretOutputs_connectionString: postgres2_kv.getSecret('connectionString')
        cosmos_secretOutputs_connectionString: cosmos_kv.getSecret('connectionString')
        storage_outputs_blobEndpoint: storage.outputs.blobEndpoint
        storage_outputs_tableEndpoint: storage.outputs.tableEndpoint
        storage_outputs_queueEndpoint: storage.outputs.queueEndpoint
        kv3_outputs_vaultUri: kv3.outputs.vaultUri
        appConfig_outputs_appConfigEndpoint: appConfig.outputs.appConfigEndpoint
        ai_outputs_appInsightsConnectionString: ai.outputs.appInsightsConnectionString
        redis_secretOutputs_connectionString: redis_kv.getSecret('connectionString')
        sb_outputs_serviceBusEndpoint: sb.outputs.serviceBusEndpoint
        signalr_outputs_hostName: signalr.outputs.hostName
        test_outputs_test: test.outputs.test
        test_outputs_val0: test.outputs.val0
        test_outputs_val1: test.outputs.val1
        default_identity_outputs_id: default_identity.outputs.id
        default_identity_outputs_clientId: default_identity.outputs.clientId
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        api_containerImage: inputs.api.containerImage
    }
}

resource postgres2_kv 'Microsoft.KeyVault/vaults@2022-02-01-preview' = {
    name: 'kv-postgres2-${uniqueString(resourceGroup().id)}'
    location: location
    properties: {
        sku: {
            family: 'A'
            name: 'standard'
        }
        tenantId: subscription().tenantId
        enabledForDeployment: true
        accessPolicies: []
    }
    tags: tags
}

resource cosmos_kv 'Microsoft.KeyVault/vaults@2022-02-01-preview' = {
    name: 'kv-cosmos-${uniqueString(resourceGroup().id)}'
    location: location
    properties: {
        sku: {
            family: 'A'
            name: 'standard'
        }
        tenantId: subscription().tenantId
        enabledForDeployment: true
        accessPolicies: []
    }
    tags: tags
}

resource redis_kv 'Microsoft.KeyVault/vaults@2022-02-01-preview' = {
    name: 'kv-redis-${uniqueString(resourceGroup().id)}'
    location: location
    properties: {
        sku: {
            family: 'A'
            name: 'standard'
        }
        tenantId: subscription().tenantId
        enabledForDeployment: true
        accessPolicies: []
    }
    tags: tags
}

