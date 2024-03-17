param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
param param_1 string // {containerRegistry.outputs.loginServer}
param param_2 string // {containerRegistry.outputs.mid}
param param_3 string // {api.containerImage}
param param_4 string // {storage.outputs.blobEndpoint}
param param_5 string // {default-identity.outputs.id}
param param_6 string // {default-identity.outputs.clientId}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'api'
    location: location
    tags: tags
    identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${param_5}': {} }
}
    properties: {
        environmentId: param_0
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
  external: true
  targetPort: 8080
  transport: 'http'
additionalPortMappings: [
{
  external: false
  targetPort: 1034
}
]
}

            registries: [ {
    server: param_1
    identity: param_2
} ]
            secrets: [
{ name: 'connectionstrings--blobs', value: param_4 }
]

        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: param_3
                    name: 'api'
                    env: [
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
{ name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
{ name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
{ name: 'ConnectionStrings__blobs', secretRef: 'connectionstrings--blobs' }
{ name: 'AZURE_CLIENT_ID', value: param_6 }
]

                }
            ]
        }
    }
}