param location string
param tags object = {}
@secure()
param mysql_password_value string
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'mysql'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 3306
                transport: 'tcp'
            }
            secrets: [
                { name: 'mysql_root_password', value: mysql_password_value }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'mysql:8.3.0'
                    name: 'mysql'
                    env: [
                        { name: 'MYSQL_ROOT_PASSWORD', secretRef: 'mysql_root_password' }
                        { name: 'MYSQL_DATABASE', value: 'mysqldb' }
                    ]
                }
            ]
        }
    }
}
