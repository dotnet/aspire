param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'mongo'
    location: location
    tags: tags
    
    properties: {
        environmentId: param_0
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
                    name: 'mongo'
                    
                }
            ]
        }
    }
}