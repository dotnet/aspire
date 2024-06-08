param location string
param tags object = {}
@secure()
param rabbitmq_password_value string
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'messaging'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 15672
                transport: 'http'
                additionalPortMappings: [
                    {
                        external: false
                        targetPort: 5672
                    }
                ]
            }
            secrets: [
                { name: 'rabbitmq_default_pass', value: rabbitmq_password_value }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'docker.io/library/rabbitmq:3.13-management'
                    name: 'messaging'
                    env: [
                        { name: 'RABBITMQ_DEFAULT_USER', value: 'guest' }
                        { name: 'RABBITMQ_DEFAULT_PASS', secretRef: 'rabbitmq_default_pass' }
                    ]
                }
            ]
        }
    }
}
