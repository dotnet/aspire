param location string
param tags object = {}
param sql_outputs_sqlServerFqdn string
@secure()
param postgres2_secretOutputs_connectionString string
@secure()
param cosmos_secretOutputs_connectionString string
param storage_outputs_blobEndpoint string
param storage_outputs_tableEndpoint string
param storage_outputs_queueEndpoint string
param kv3_outputs_vaultUri string
param appConfig_outputs_appConfigEndpoint string
param ai_outputs_appInsightsConnectionString string
@secure()
param redis_secretOutputs_connectionString string
param sb_outputs_serviceBusEndpoint string
param signalr_outputs_hostName string
param test_outputs_test string
param test_outputs_val0 string
param test_outputs_val1 string
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
                { name: 'connectionstrings--db', value: 'Server=tcp:${sql_outputs_sqlServerFqdn},1433;Encrypt=True;Authentication="Active Directory Default";Database=db' }
                { name: 'connectionstrings--db2', value: '${postgres2_secretOutputs_connectionString};Database=db2' }
                { name: 'connectionstrings--cosmos', value: cosmos_secretOutputs_connectionString }
                { name: 'connectionstrings--blob', value: storage_outputs_blobEndpoint }
                { name: 'connectionstrings--table', value: storage_outputs_tableEndpoint }
                { name: 'connectionstrings--queue', value: storage_outputs_queueEndpoint }
                { name: 'connectionstrings--kv3', value: kv3_outputs_vaultUri }
                { name: 'connectionstrings--appconfig', value: appConfig_outputs_appConfigEndpoint }
                { name: 'applicationinsights_connection_string', value: ai_outputs_appInsightsConnectionString }
                { name: 'connectionstrings--redis', value: redis_secretOutputs_connectionString }
                { name: 'connectionstrings--sb', value: sb_outputs_serviceBusEndpoint }
                { name: 'connectionstrings--signalr', value: 'Endpoint=https://${signalr_outputs_hostName};AuthType=azure' }
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
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY', value: 'in_memory' }
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
                        { name: 'bicepValue_test', value: test_outputs_test }
                        { name: 'bicepValue0', value: test_outputs_val0 }
                        { name: 'bicepValue1', value: test_outputs_val1 }
                        { name: 'AZURE_CLIENT_ID', value: default_identity_outputs_clientId }
                    ]
                }
            ]
        }
    }
}
