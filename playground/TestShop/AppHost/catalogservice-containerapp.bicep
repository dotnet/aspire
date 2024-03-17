param location string
param tags object = {}
@secure()
param postgres_password_value string
param containerAppEnv_outputs_id string
param containerRegistry_outputs_loginServer string
param containerRegistry_outputs_mid string
param catalogservice_containerImage string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'catalogservice'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                  external: false
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
                { name: 'connectionstrings--catalogdb', value: 'Host=postgres;Port=5432;Username=postgres;Password=${postgres_password_value};Database=catalogdb' }
            ]
        }
        template: {
            scale: {
                minReplicas: 2
            }
            containers: [
                {
                    image: catalogservice_containerImage
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
