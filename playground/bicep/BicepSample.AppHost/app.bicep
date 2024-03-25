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

module test0 'test0.bicep' = {
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
        principalId: default_identity.outputs.principalId
        keyVaultName: postgres2_kv.name
        administratorLogin: parameters.administratorLogin
        administratorLoginPassword: parameters.administratorLoginPassword
        principalType: 'ServicePrincipal'
    }
}

module cosmos 'cosmos.module.bicep' = {
    name: 'cosmos'
    params: {
        location: location
        keyVaultName: cosmos_kv.name
    }
}

module ai 'ai.module.bicep' = {
    name: 'ai'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        principalType: 'ServicePrincipal'
        logAnalyticsWorkspaceId: containerAppEnv.outputs.logAnalyticsWorkspaceId
    }
}

module redis 'redis.module.bicep' = {
    name: 'redis'
    params: {
        location: location
        principalId: default_identity.outputs.principalId
        keyVaultName: redis_kv.name
        principalType: 'ServicePrincipal'
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

module containerAppEnv 'containerappenv.bicep' = {
    name: 'containerAppEnv'
    params: {
        location: location
    }
}

module containerRegistry 'containerregistry.bicep' = {
    name: 'containerRegistry'
    params: {
        location: location
    }
}

module default_identity 'default-identity.bicep' = {
    name: 'default-identity'
    params: {
        location: location
    }
}

module api_containerApp 'api-containerapp.bicep' = {
    name: 'api-containerApp'
    params: {
        location: location
        param_0: sql.outputs.sqlServerFqdn
        param_1: postgres2_kv.getSecret('connectionString')
        param_2: cosmos_kv.getSecret('connectionString')
        param_3: storage.outputs.blobEndpoint
        param_4: storage.outputs.tableEndpoint
        param_5: storage.outputs.queueEndpoint
        param_6: kv3.outputs.vaultUri
        param_7: appConfig.outputs.appConfigEndpoint
        param_8: ai.outputs.appInsightsConnectionString
        param_9: redis_kv.getSecret('connectionString')
        param_10: sb.outputs.serviceBusEndpoint
        param_11: signalr.outputs.hostName
        param_12: test.outputs.test
        param_13: test.outputs.val0
        param_14: test.outputs.val1
        param_15: default_identity.outputs.id
        param_16: default_identity.outputs.clientId
        param_17: containerAppEnv.outputs.id
        param_18: containerRegistry.outputs.loginServer
        param_19: containerRegistry.outputs.mid
        param_20: inputs.api.containerImage
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

