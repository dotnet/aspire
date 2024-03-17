param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
param param_1 string // {containerRegistry.outputs.loginServer}
param param_2 string // {containerRegistry.outputs.mid}
param param_3 string // {api.containerImage}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'api'
    location: location
    tags: tags
    
    properties: {
        environmentId: param_0
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
    external: false
    targetPort: 8080
    transport: 'http'
}
            registries: [ {
    server: param_1
    identity: param_2
} ]
            secrets: [
{ name: 'connectionstrings--mongo', value: 'mongodb://mongo:27017' }
]

        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: param_3
                    name: 'api'
                    env: [
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
{ name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
{ name: 'ConnectionStrings__mongo', secretRef: 'connectionstrings--mongo' }
]

                }
            ]
        }
    }
}