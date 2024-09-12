param principalId string

param principalName string

resource sql 'Microsoft.Sql/servers@2020-11-01-preview' = {
    name: take('sql-${uniqueString(resourceGroup().id)}', 63)
    location: resourceGroup().location
    properties: {
        administrators: {
            administratorType: 'ActiveDirectory'
            login: principalName
            sid: principalId
            tenantId: subscription().tenantId
            azureADOnlyAuthentication: true
        }
        minimalTlsVersion: '1.2'
        publicNetworkAccess: 'Enabled'
        version: '12.0'
    }
    tags: {
        'aspire-resource-name': 'sql'
    }
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
    name: 'AllowAllAzureIps'
    parent: sql
}

resource sqldb 'Microsoft.Sql/servers/databases@2021-11-01' = {
    name: 'sqldb'
    location: resourceGroup().location
    parent: sql
}

output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName