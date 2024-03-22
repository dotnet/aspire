param location string
param tags object = {}
@secure()
param param_0 string // {postgres.inputs.password}
param param_1 string // {containerAppEnv.outputs.id}
param param_2 string // {containerRegistry.outputs.loginServer}
param param_3 string // {containerRegistry.outputs.mid}
param param_4 string // {catalogservice.containerImage}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'catalogservice'
    location: location
    tags: tags
    
    properties: {
        environmentId: param_1
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
  external: false
  targetPort: 8080
  transport: 'http'
}

            registries: [ {
    server: param_2
    identity: param_3
} ]
            secrets: [
{ name: 'connectionstrings--catalogdb', value: 'Host=postgres;Port=5432;Username=postgres;Password=${param_0};Database=catalogdb' }
]

        }
        template: {
            scale: {
                minReplicas: 2
            }
            containers: [
                {
                    image: param_4
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