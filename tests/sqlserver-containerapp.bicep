param location string
param tags object = {}
@secure()
param sqlserver_password_value string
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'sqlserver'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 1433
                transport: 'tcp'
            }
            secrets: [
                { name: 'mssql_sa_password', value: sqlserver_password_value }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'mcr.microsoft.com/mssql/server:2022-latest'
                    name: 'sqlserver'
                    env: [
                        { name: 'ACCEPT_EULA', value: 'Y' }
                        { name: 'MSSQL_SA_PASSWORD', secretRef: 'mssql_sa_password' }
                    ]
                }
            ]
        }
    }
}
