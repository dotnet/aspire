param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
param param_1 string // {containerRegistry.outputs.loginServer}
param param_2 string // {containerRegistry.outputs.mid}
param param_3 string // {api.containerImage}
param param_4 string // {sql.outputs.sqlServerFqdn}
@secure()
param param_5 string // {postgres2.secretOutputs.connectionString}
@secure()
param param_6 string // {cosmos.secretOutputs.connectionString}
param param_7 string // {storage.outputs.blobEndpoint}
param param_8 string // {storage.outputs.tableEndpoint}
param param_9 string // {storage.outputs.queueEndpoint}
param param_10 string // {kv3.outputs.vaultUri}
param param_11 string // {appConfig.outputs.appConfigEndpoint}
param param_12 string // {ai.outputs.appInsightsConnectionString}
@secure()
param param_13 string // {redis.secretOutputs.connectionString}
param param_14 string // {sb.outputs.serviceBusEndpoint}
param param_15 string // {signalr.outputs.hostName}
param param_16 string // {test.outputs.test}
param param_17 string // {test.outputs.val0}
param param_18 string // {test.outputs.val1}
param param_19 string // {default-identity.outputs.id}
param param_20 string // {default-identity.outputs.clientId}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'api'
    location: location
    tags: tags
    identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${param_19}': {} }
}
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
{ name: 'connectionstrings--db', value: 'Server=tcp:${param_4},1433;Encrypt=True;Authentication="Active Directory Default";Database=db' }
{ name: 'connectionstrings--db2', value: '${param_5};Database=db2' }
{ name: 'connectionstrings--cosmos', value: param_6 }
{ name: 'connectionstrings--blob', value: param_7 }
{ name: 'connectionstrings--table', value: param_8 }
{ name: 'connectionstrings--queue', value: param_9 }
{ name: 'connectionstrings--kv3', value: param_10 }
{ name: 'connectionstrings--appconfig', value: param_11 }
{ name: 'applicationinsights_connection_string', value: param_12 }
{ name: 'connectionstrings--redis', value: param_13 }
{ name: 'connectionstrings--sb', value: param_14 }
{ name: 'connectionstrings--signalr', value: 'Endpoint=https://${param_15};AuthType=azure' }
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
{ name: 'ConnectionStrings__db', secretRef: 'connectionstrings--db' }
{ name: 'ConnectionStrings__db2', secretRef: 'connectionstrings--db2' }
{ name: 'ConnectionStrings__cosmos', secretRef: 'connectionstrings--cosmos' }
{ name: 'ConnectionStrings__blob', secretRef: 'connectionstrings--blob' }
{ name: 'ConnectionStrings__table', secretRef: 'connectionstrings--table' }
{ name: 'ConnectionStrings__queue', secretRef: 'connectionstrings--queue' }
{ name: 'ConnectionStrings__kv3', secretRef: 'connectionstrings--kv3' }
{ name: 'ConnectionStrings__appConfig', secretRef: 'connectionstrings--appconfig' }
{ name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', secretRef: 'applicationinsights_connection_string' }
{ name: 'ConnectionStrings__redis', secretRef: 'connectionstrings--redis' }
{ name: 'ConnectionStrings__sb', secretRef: 'connectionstrings--sb' }
{ name: 'ConnectionStrings__signalr', secretRef: 'connectionstrings--signalr' }
{ name: 'bicepValue_test', value: param_16 }
{ name: 'bicepValue0', value: param_17 }
{ name: 'bicepValue1', value: param_18 }
{ name: 'AZURE_CLIENT_ID', value: param_20 }
]

                }
            ]
        }
    }
}