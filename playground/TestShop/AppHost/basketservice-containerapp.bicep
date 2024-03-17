param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
param param_1 string // {containerRegistry.outputs.loginServer}
param param_2 string // {containerRegistry.outputs.mid}
param param_3 string // {basketservice.containerImage}
@secure()
param param_4 string // {messaging.inputs.password}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'basketservice'
    location: location
    tags: tags
    
    properties: {
        environmentId: param_0
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
    external: false
    targetPort: 8080
    transport: 'http2'
}
            registries: [ {
    server: param_1
    identity: param_2
} ]
            secrets: [
{ name: 'connectionstrings--basketcache', value: 'basketcache:6379' }
{ name: 'connectionstrings--messaging', value: 'amqp://guest:${param_4}@messaging:5672' }
]

        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: param_3
                    name: 'basketservice'
                    env: [
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
{ name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
{ name: 'ConnectionStrings__basketcache', secretRef: 'connectionstrings--basketcache' }
{ name: 'ConnectionStrings__messaging', secretRef: 'connectionstrings--messaging' }
]

                }
            ]
        }
    }
}