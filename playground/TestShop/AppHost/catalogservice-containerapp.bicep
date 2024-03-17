param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
param param_1 string // {containerRegistry.outputs.loginServer}
param param_2 string // {containerRegistry.outputs.mid}
param param_3 string // {catalogservice.containerImage}
@secure()
param param_4 string // {postgres.inputs.password}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'catalogservice'
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
{ name: 'connectionstrings--catalogdb', value: 'Host=postgres;Port=5432;Username=postgres;Password=${param_4};Database=catalogdb' }
]

        }
        template: {
            scale: {
                minReplicas: 2
            }
            containers: [
                {
                    image: param_3
                    name: 'catalogservice'
                    env: [
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
{ name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
{ name: 'ConnectionStrings__catalogdb', secretRef: 'connectionstrings--catalogdb' }
]

                }
            ]
        }
    }
}