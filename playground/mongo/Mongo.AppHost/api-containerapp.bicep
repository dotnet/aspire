param location string
param tags object = {}
param containerAppEnv_outputs_id string
param containerRegistry_outputs_loginServer string
param containerRegistry_outputs_mid string
param api_containerImage string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'api'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: true
                targetPort: 8080
                transport: 'http'
            }
            registries: [
                {
                    server: containerRegistry_outputs_loginServer
                    identity: containerRegistry_outputs_mid
                }
            ]
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
                    image: api_containerImage
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
