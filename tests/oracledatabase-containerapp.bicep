param location string
param tags object = {}
@secure()
param oracledatabase_password_value string
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'oracledatabase'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 1521
                transport: 'tcp'
            }
            secrets: [
                { name: 'oracle_pwd', value: oracledatabase_password_value }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'container-registry.oracle.com/database/free:23.3.0.0'
                    name: 'oracledatabase'
                    env: [
                        { name: 'ORACLE_PWD', secretRef: 'oracle_pwd' }
                    ]
                }
            ]
        }
    }
}
