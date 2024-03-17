param location string
param tags object = {}
param param_0 string // {containerAppEnv.outputs.id}
@secure()
param param_1 string // {postgres.inputs.password}

resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
    name: 'postgres'
    location: location
    tags: tags
    
    properties: {
        environmentId: param_0
        configuration: {
            activeRevisionsMode: 'Single'
            ingress: {
    external: false
    targetPort: 5432
    transport: 'tcp'
}
            
            secrets: [
{ name: 'postgres_password', value: param_1 }
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
{ name: 'POSTGRES_PASSWORD', secretRef: 'postgres_password' }
]

                }
            ]
        }
    }
}