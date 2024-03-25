param location string
param tags object = {}
@secure()
param param_0 string // {messaging-password.value}
param param_1 string // {containerAppEnv.outputs.id}
param param_2 string // {containerRegistry.outputs.loginServer}
param param_3 string // {containerRegistry.outputs.mid}
param param_4 string // {orderprocessor.containerImage}
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'orderprocessor'
    location: location
    tags: tags
    properties: {
        environmentId: param_1
        configuration: {
            activeRevisionsMode: 'Single'
            registries: [ {
                server: param_2
                identity: param_3
            } ]
            secrets: [
                { name: 'connectionstrings--messaging', value: 'amqp://guest:${param_0}@messaging:5672' }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: param_4
                    name: 'orderprocessor'
                    env: [
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'ConnectionStrings__messaging', secretRef: 'connectionstrings--messaging' }
                    ]
                }
            ]
        }
    }
}
