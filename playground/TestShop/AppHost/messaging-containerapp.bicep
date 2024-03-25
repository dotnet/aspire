param location string
param tags object = {}
@secure()
param param_0 string // {messaging-password.value}
param param_1 string // {containerAppEnv.outputs.id}
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'messaging'
    location: location
    tags: tags
    properties: {
        environmentId: param_1
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                  external: false
                  targetPort: 5672
                  transport: 'tcp'
            }
            secrets: [
                { name: 'rabbitmq_default_pass', value: param_0 }
            ]
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'rabbitmq:3'
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
