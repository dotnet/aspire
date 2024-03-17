param location string
param tags object = {}
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'mongodb'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                external: false
                targetPort: 27017
                transport: 'tcp'
            }
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'mongo:7.0.5'
                    name: 'mongodb'
                }
            ]
        }
    }
}
