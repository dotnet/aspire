resource mykv 'Microsoft.KeyVault/vaults@2019-09-01' = {
    name: take('mykv-${uniqueString(resourceGroup().id)}', 24)
    location: resourceGroup().location
    properties: { }
    tags: {
        'aspire-resource-name': 'mykv'
    }
}

output vaultUri string = mykv.properties.vaultUri

resource KeyVaultAdministrator_mykv 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(resourceGroup().id, 'KeyVaultAdministrator_mykv')
    properties: {
        principalId: principalId
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
        principalType: principalType
    }
    scope: mykv
}

resource mysecret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
    name: take('mysecret-${uniqueString(resourceGroup().id)}', 127)
    properties: {
        value: signaturesecret
    }
    parent: mykv
}

param principalId string

param principalType string