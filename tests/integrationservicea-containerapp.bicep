param location string
param tags object = {}
@secure()
param sqlserver_password_value string
@secure()
param mysql_password_value string
@secure()
param postgres_password_value string
@secure()
param rabbitmq_password_value string
@secure()
param oracledatabase_password_value string
@secure()
param cosmos_secretOutputs_connectionString string
param containerAppEnv_outputs_id string
param containerRegistry_outputs_loginServer string
param containerRegistry_outputs_mid string
param integrationservicea_containerImage string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'integrationservicea'
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
                { name: 'connectionstrings--tempdb', value: 'Server=sqlserver,1433;User ID=sa;Password=${sqlserver_password_value};TrustServerCertificate=true;Database=tempdb' }
                { name: 'connectionstrings--mysqldb', value: 'Server=mysql;Port=3306;User ID=root;Password=${mysql_password_value};Database=mysqldb' }
                { name: 'connectionstrings--redis', value: 'redis:6379' }
                { name: 'connectionstrings--postgresdb', value: 'Host=postgres;Port=5432;Username=postgres;Password=${postgres_password_value};Database=postgresdb' }
                { name: 'connectionstrings--rabbitmq', value: 'amqp://guest:${rabbitmq_password_value}@rabbitmq:5672' }
                { name: 'connectionstrings--mymongodb', value: 'mongodb://mongodb:27017/mymongodb' }
                { name: 'connectionstrings--freepdb1', value: 'user id=system;password=${oracledatabase_password_value};data source=oracledatabase:1521/freepdb1' }
                { name: 'connectionstrings--kafka', value: 'kafka:9092' }
                { name: 'connectionstrings--cosmos', value: cosmos_secretOutputs_connectionString }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: integrationservicea_containerImage
                    name: 'integrationservicea'
                    env: [
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES', value: 'true' }
                        { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
                        { name: 'SKIP_RESOURCES', value: '' }
                        { name: 'ConnectionStrings__tempdb', secretRef: 'connectionstrings--tempdb' }
                        { name: 'ConnectionStrings__mysqldb', secretRef: 'connectionstrings--mysqldb' }
                        { name: 'ConnectionStrings__redis', secretRef: 'connectionstrings--redis' }
                        { name: 'ConnectionStrings__postgresdb', secretRef: 'connectionstrings--postgresdb' }
                        { name: 'ConnectionStrings__rabbitmq', secretRef: 'connectionstrings--rabbitmq' }
                        { name: 'ConnectionStrings__mymongodb', secretRef: 'connectionstrings--mymongodb' }
                        { name: 'ConnectionStrings__freepdb1', secretRef: 'connectionstrings--freepdb1' }
                        { name: 'ConnectionStrings__kafka', secretRef: 'connectionstrings--kafka' }
                        { name: 'ConnectionStrings__cosmos', secretRef: 'connectionstrings--cosmos' }
                    ]
                }
            ]
        }
    }
}
