param location string
param tags object = {}
param storage_outputs_blobEndpoint string
param default_identity_outputs_id string
param default_identity_outputs_clientId string
param containerAppEnv_outputs_id string
param containerRegistry_outputs_loginServer string
param containerRegistry_outputs_mid string
param api_containerImage string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'api'
    location: location
    tags: tags
    identity: {
        type: 'UserAssigned'
        userAssignedIdentities: {
            '${default_identity_outputs_id}': {}
        }
    }
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 8080
                transport: 'http'
                additionalPortMappings: [
                    {
                        external: false
                        targetPort: 1034
                    }
                ]
            }
            registries: [
                {
                    server: containerRegistry_outputs_loginServer
                    identity: containerRegistry_outputs_mid
                }
            ]
            secrets: [
                { name: 'connectionstrings--blobs', value: storage_outputs_blobEndpoint }
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
                        { name: 'ConnectionStrings__blobs', secretRef: 'connectionstrings--blobs' }
                        { name: 'URL', value: 'http://api:1034' }
                        { name: 'AZURE_CLIENT_ID', value: default_identity_outputs_clientId }
                    ]
                }
            ]
        }
    }
}
