param location string
param tags object = {}
param parameters object = {}
param inputs object = {}

module postgres_containerApp 'postgres-containerapp.module.bicep' = {
    name: 'postgres-containerApp'
    params: {
        location: location
        postgres_password_value: parameters.postgres_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module basketcache_containerApp 'basketcache-containerapp.module.bicep' = {
    name: 'basketcache-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module messaging_containerApp 'messaging-containerapp.module.bicep' = {
    name: 'messaging-containerApp'
    params: {
        location: location
        rabbitmq_password_value: parameters.rabbitmq_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
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

module catalogservice_containerApp 'catalogservice-containerapp.module.bicep' = {
    name: 'catalogservice-containerApp'
    params: {
        location: location
        postgres_password_value: parameters.postgres_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        catalogservice_containerImage: inputs.catalogservice.containerImage
    }
}

module basketservice_containerApp 'basketservice-containerapp.module.bicep' = {
    name: 'basketservice-containerApp'
    params: {
        location: location
        rabbitmq_password_value: parameters.rabbitmq_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        basketservice_containerImage: inputs.basketservice.containerImage
    }
}

module frontend_containerApp 'frontend-containerapp.module.bicep' = {
    name: 'frontend-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_defaultDomain: containerAppEnv.outputs.defaultDomain
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        frontend_containerImage: inputs.frontend.containerImage
    }
}

module orderprocessor_containerApp 'orderprocessor-containerapp.module.bicep' = {
    name: 'orderprocessor-containerApp'
    params: {
        location: location
        rabbitmq_password_value: parameters.rabbitmq_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        orderprocessor_containerImage: inputs.orderprocessor.containerImage
    }
}

module apigateway_containerApp 'apigateway-containerapp.module.bicep' = {
    name: 'apigateway-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_defaultDomain: containerAppEnv.outputs.defaultDomain
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        apigateway_containerImage: inputs.apigateway.containerImage
    }
}

module catalogdbapp_containerApp 'catalogdbapp-containerapp.module.bicep' = {
    name: 'catalogdbapp-containerApp'
    params: {
        location: location
        postgres_password_value: parameters.postgres_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        catalogdbapp_containerImage: inputs.catalogdbapp.containerImage
    }
}

