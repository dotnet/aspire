param location string
param tags object = {}
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'kafka'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 9092
                transport: 'tcp'
            }
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'confluentinc/confluent-local:7.6.0'
                    name: 'kafka'
                    env: [
                        { name: 'KAFKA_ADVERTISED_LISTENERS', value: 'PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:9092' }
                    ]
                }
            ]
        }
    }
}
