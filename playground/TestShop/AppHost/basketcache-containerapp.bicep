param location string
param tags object = {}
param containerAppEnv_outputs_id string
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'basketcache'
    location: location
    tags: tags
    properties: {
        environmentId: containerAppEnv_outputs_id
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
                  external: false
                  targetPort: 6379
                  transport: 'tcp'
            }
        }
        template: {
            scale: {
                minReplicas: 1
            }
            containers: [
                {
                    image: 'redis:7.2.4'
                    name: 'basketcache'
                }
            ]
        }
    }
}
