param location string
param tags object = {}
param parameters object = {}
param inputs object = {}
module mongo_containerApp 'mongo-containerapp.bicep' = {
    name: 'mongo-containerApp'
    params: {
        location: location
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

module api_containerApp 'api-containerapp.bicep' = {
    name: 'api-containerApp'
    params: {
        location: location
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        api_containerImage: inputs.api.containerImage
    }
}

