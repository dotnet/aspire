param location string
param tags object = {}
param parameters object = {}
param inputs object = {}
module storage 'storage.module.bicep' = {
    name: 'storage'
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
        storage_outputs_blobEndpoint: storage.outputs.blobEndpoint
        default_identity_outputs_id: default_identity.outputs.id
        default_identity_outputs_clientId: default_identity.outputs.clientId
        containerAppEnv_outputs_id: containerAppEnv.outputs.id
        containerRegistry_outputs_loginServer: containerRegistry.outputs.loginServer
        containerRegistry_outputs_mid: containerRegistry.outputs.mid
        api_containerImage: inputs.api.containerImage
    }
}

