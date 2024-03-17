param location string
param tags object = {}
@secure()
param postgres_password_value string
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'postgres'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 5432
                transport: 'tcp'
            }
            secrets: [
                { name: 'postgres_password', value: postgres_password_value }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'postgres:16.2'
                    name: 'postgres'
                    env: [
                        { name: 'POSTGRES_HOST_AUTH_METHOD', value: 'scram-sha-256' }
                        { name: 'POSTGRES_INITDB_ARGS', value: '--auth-host=scram-sha-256 --auth-local=scram-sha-256' }
                        { name: 'POSTGRES_USER', value: 'postgres' }
                        { name: 'POSTGRES_PASSWORD', secretRef: 'postgres_password' }
                        { name: 'POSTGRES_DB', value: 'postgresdb' }
                    ]
                }
            ]
        }
    }
}
