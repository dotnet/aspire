@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalName string

resource sql 'Microsoft.Sql/servers@2021-11-01' = {
    name: toLower(take('sql${uniqueString(resourceGroup().id)}', 24))
    location: location
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

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
    name: 'AllowAllAzureIps'
    properties: {
        endIpAddress: '0.0.0.0'
        startIpAddress: '0.0.0.0'
    }
    parent: sql
}

resource db 'Microsoft.Sql/servers/databases@2021-11-01' = {
    name: 'db'
    location: location
    parent: sql
}

output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName