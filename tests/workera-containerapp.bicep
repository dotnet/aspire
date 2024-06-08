param location string
param tags object = {}
param containerAppEnv_outputs_id string
param containerRegistry_outputs_loginServer string
param containerRegistry_outputs_mid string
param workera_containerImage string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'workera'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            registries: [
                {
                    server: containerRegistry_outputs_loginServer
                    identity: containerRegistry_outputs_mid
                }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: workera_containerImage
                    name: 'workera'
                    env: [
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
                    ]
                }
            ]
        }
    }
}
