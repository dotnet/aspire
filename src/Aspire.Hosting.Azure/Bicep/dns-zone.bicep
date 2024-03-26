@description('The name fo the DNS zone to be created. Must have at least 2 segments separated by a period')
param zoneName string

@description('Tags that will be applied to all resources')
param tags object = {}

param principalId string
param principalType string = 'ServicePrincipal'

var resourceToken = uniqueString(resourceGroup().id)

resource dnsZone 'Microsoft.Dns/dnsZones@2018-05-01' = {
    name: zoneName
    location: 'global'
    tags: tags
}

resource DnsZoneContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(dnsZone.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'befefa01-2a29-4197-83a8-272ff33ce314'))
    scope: dnsZone
    properties: {
        principalId: principalId
        principalType: principalType
        roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'befefa01-2a29-4197-83a8-272ff33ce314')
    }
}

output nameservers array = zone.properties.nameServers
output zoneName string = dnsZone.name
