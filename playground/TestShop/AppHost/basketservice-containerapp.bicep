param location string
param tags object = {}
@secure()
param messaging_password_value string
param containerAppEnv_outputs_id string
param containerRegistry_outputs_loginServer string
param containerRegistry_outputs_mid string
param basketservice_containerImage string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'basketservice'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                  external: false
                  targetPort: 8080
                  transport: 'http2'
            }
            registries: [
                {
                    server: containerRegistry_outputs_loginServer
                    identity: containerRegistry_outputs_mid
                }
            ]
            secrets: [
                { name: 'connectionstrings--basketcache', value: 'basketcache:6379' }
                { name: 'connectionstrings--messaging', value: 'amqp://guest:${messaging_password_value}@messaging:5672' }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: basketservice_containerImage
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
