param location string
param tags object = {}
param parameters object = {}
param inputs object = {}
module sqlserver_containerApp 'sqlserver-containerapp.bicep' = {
    name: 'sqlserver-containerApp'
    params: {
        location: location
        sqlserver_password_value: parameters.sqlserver_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module mysql_containerApp 'mysql-containerapp.bicep' = {
    name: 'mysql-containerApp'
    params: {
        location: location
        mysql_password_value: parameters.mysql_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module redis_containerApp 'redis-containerapp.bicep' = {
    name: 'redis-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module postgres_containerApp 'postgres-containerapp.bicep' = {
    name: 'postgres-containerApp'
    params: {
        location: location
        postgres_password_value: parameters.postgres_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module rabbitmq_containerApp 'rabbitmq-containerapp.bicep' = {
    name: 'rabbitmq-containerApp'
    params: {
        location: location
        rabbitmq_password_value: parameters.rabbitmq_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module mongodb_containerApp 'mongodb-containerapp.bicep' = {
    name: 'mongodb-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module oracledatabase_containerApp 'oracledatabase-containerapp.bicep' = {
    name: 'oracledatabase-containerApp'
    params: {
        location: location
        oracledatabase_password_value: parameters.oracledatabase_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module kafka_containerApp 'kafka-containerapp.bicep' = {
    name: 'kafka-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module cosmos 'cosmos.module.bicep' = {
    name: 'cosmos'
    params: {
        location: location
        keyVaultName: cosmos_kv.name
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

module servicea_containerApp 'servicea-containerapp.bicep' = {
    name: 'servicea-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        servicea_containerImage: inputs.servicea.containerImage
    }
}

module serviceb_containerApp 'serviceb-containerapp.bicep' = {
    name: 'serviceb-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        serviceb_containerImage: inputs.serviceb.containerImage
    }
}

module servicec_containerApp 'servicec-containerapp.bicep' = {
    name: 'servicec-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        servicec_containerImage: inputs.servicec.containerImage
    }
}

module workera_containerApp 'workera-containerapp.bicep' = {
    name: 'workera-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        workera_containerImage: inputs.workera.containerImage
    }
}

module integrationservicea_containerApp 'integrationservicea-containerapp.bicep' = {
    name: 'integrationservicea-containerApp'
    params: {
        location: location
        sqlserver_password_value: parameters.sqlserver_password
        mysql_password_value: parameters.mysql_password
        postgres_password_value: parameters.postgres_password
        rabbitmq_password_value: parameters.rabbitmq_password
        oracledatabase_password_value: parameters.oracledatabase_password
        cosmos_secretOutputs_connectionString: cosmos_kv.getSecret('connectionString')
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        integrationservicea_containerImage: inputs.integrationservicea.containerImage
    }
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

