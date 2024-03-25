param location string
param tags object = {}
param parameters object = {}
param inputs object = {}
module postgres_containerApp 'postgres-containerapp.bicep' = {
    name: 'postgres-containerApp'
    params: {
        location: location
        postgres_password_value: parameters.postgres_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module basketcache_containerApp 'basketcache-containerapp.bicep' = {
    name: 'basketcache-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
    }
}

module messaging_containerApp 'messaging-containerapp.bicep' = {
    name: 'messaging-containerApp'
    params: {
        location: location
        messaging_password_value: parameters.messaging_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
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

module catalogservice_containerApp 'catalogservice-containerapp.bicep' = {
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

module basketservice_containerApp 'basketservice-containerapp.bicep' = {
    name: 'basketservice-containerApp'
    params: {
        location: location
        messaging_password_value: parameters.messaging_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        basketservice_containerImage: inputs.basketservice.containerImage
    }
}

module frontend_containerApp 'frontend-containerapp.bicep' = {
    name: 'frontend-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        frontend_containerImage: inputs.frontend.containerImage
    }
}

module orderprocessor_containerApp 'orderprocessor-containerapp.bicep' = {
    name: 'orderprocessor-containerApp'
    params: {
        location: location
        messaging_password_value: parameters.messaging_password
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        orderprocessor_containerImage: inputs.orderprocessor.containerImage
    }
}

module apigateway_containerApp 'apigateway-containerapp.bicep' = {
    name: 'apigateway-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        apigateway_containerImage: inputs.apigateway.containerImage
    }
}

module catalogdbapp_containerApp 'catalogdbapp-containerapp.bicep' = {
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

