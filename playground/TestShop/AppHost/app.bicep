param location string
param tags object = {}
param parameters object = {}
param inputs object = {}
module postgres_containerApp 'postgres-containerapp.bicep' = {
    name: 'postgres-containerApp'
    params: {
        location: location
        param_0: parameters.postgres_password
        param_1: containerAppEnv.outputs.id
    }
}

module basketcache_containerApp 'basketcache-containerapp.bicep' = {
    name: 'basketcache-containerApp'
    params: {
        location: location
        param_0: containerAppEnv.outputs.id
    }
}

module messaging_containerApp 'messaging-containerapp.bicep' = {
    name: 'messaging-containerApp'
    params: {
        location: location
        param_0: parameters.messaging_password
        param_1: containerAppEnv.outputs.id
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
        param_0: parameters.postgres_password
        param_1: containerAppEnv.outputs.id
        param_2: containerRegistry.outputs.loginServer
        param_3: containerRegistry.outputs.mid
        param_4: inputs.catalogservice.containerImage
    }
}

module basketservice_containerApp 'basketservice-containerapp.bicep' = {
    name: 'basketservice-containerApp'
    params: {
        location: location
        param_0: parameters.messaging_password
        param_1: containerAppEnv.outputs.id
        param_2: containerRegistry.outputs.loginServer
        param_3: containerRegistry.outputs.mid
        param_4: inputs.basketservice.containerImage
    }
}

module frontend_containerApp 'frontend-containerapp.bicep' = {
    name: 'frontend-containerApp'
    params: {
        location: location
        param_0: containerAppEnv.outputs.id
        param_1: containerRegistry.outputs.loginServer
        param_2: containerRegistry.outputs.mid
        param_3: inputs.frontend.containerImage
    }
}

module orderprocessor_containerApp 'orderprocessor-containerapp.bicep' = {
    name: 'orderprocessor-containerApp'
    params: {
        location: location
        param_0: parameters.messaging_password
        param_1: containerAppEnv.outputs.id
        param_2: containerRegistry.outputs.loginServer
        param_3: containerRegistry.outputs.mid
        param_4: inputs.orderprocessor.containerImage
    }
}

module apigateway_containerApp 'apigateway-containerapp.bicep' = {
    name: 'apigateway-containerApp'
    params: {
        location: location
        param_0: containerAppEnv.outputs.id
        param_1: containerRegistry.outputs.loginServer
        param_2: containerRegistry.outputs.mid
        param_3: inputs.apigateway.containerImage
    }
}

module catalogdbapp_containerApp 'catalogdbapp-containerapp.bicep' = {
    name: 'catalogdbapp-containerApp'
    params: {
        location: location
        param_0: parameters.postgres_password
        param_1: containerAppEnv.outputs.id
        param_2: containerRegistry.outputs.loginServer
        param_3: containerRegistry.outputs.mid
        param_4: inputs.catalogdbapp.containerImage
    }
}

