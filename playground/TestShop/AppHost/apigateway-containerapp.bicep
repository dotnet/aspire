param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
param param_1 string // {containerRegistry.outputs.loginServer}
param param_2 string // {containerRegistry.outputs.mid}
param param_3 string // {apigateway.containerImage}
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'apigateway'
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
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: param_3
                    name: 'apigateway'
                    env: [
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
                        { name: 'services__basketservice__http__0', value: 'http://basketservice' }
                        { name: 'services__basketservice__https__0', value: 'https://basketservice' }
                        { name: 'services__catalogservice__http__0', value: 'http://catalogservice' }
                        { name: 'services__catalogservice__https__0', value: 'https://catalogservice' }
                    ]
                }
            ]
        }
    }
}
