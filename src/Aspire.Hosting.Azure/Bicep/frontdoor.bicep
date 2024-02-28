param frontDoorProfileName string
param domainName string

//The following is a hacky way to get at the 'resourceToken' for the CAE name
param logAnalyticsWorkspaceId string
var resourceToken = replace(last(split(logAnalyticsWorkspaceId, '/')), 'law-', '')
var containerAppEnvName = 'cae-${resourceToken}'

param location string = resourceGroup().location

param dnsResourceGroupName string
param dnsSubscriptionId string
param dnsZoneName string
param healthProbePath string = '/health'

// Route properties
@description('How Front Door caches requests that include query strings')
@allowed([
    'IgnoreQueryString'
    'IgnoreSpecifiedQueryStrings'
    'IncludeSpecifiedQueryStrings'
    'UseQueryString'
])
param queryStringCachingBehavior string
@description('Comma-separated query parameter to include or exclude')
param queryParameters string
@description('Protocol this rule will use when forwarding traffic to backends')
@allowed([
    'HttpOnly'
    'HttpsOnly'
    'MatchRequest'
])
param forwardingProtocol string
@description('Whether to automatically redirect HTTP traffic to HTTPS traffic')
param httpsRedirect bool = true
@description('List of supported protocols for this route')
param supportedProtocols string[] = [
    'Http'
    'Https'
]

// Route cache configuration
@description('List of content types on which compression applies - the value should be a valid MIME type')
param contentTypesToCompress string[]

@description('The name of the SKU to use when creating the Front Door profile')
@allowed([
    'Standard_AzureFrontDoor'
    'Premium_AzureFrontDoor'
])
param skuName string 'Standard_AzureFrontDoor'
param allowSessionAffinity bool = false

@description('The name of the Container App resource that the Front Door should point to')
param originContainerAppName string
@description('Flag indicating that a www. subdomain should be created on the provided custom domain as well')
param createWwwSubdomainForCustomDomain bool = true
@description('The TTL value, in seconds, for the DNS records')
param dnsRecordTimeToLiveInSeconds int = 3600

///Create a valid resource name for the custom domain - resource names don't contain periods
var wwwCustomDomainResourceName = replace('www-${dnsZoneName}', '.', '-')
var wwwCnameRecordName = 'www'
var apexCustomDomainResourceName = replace('${cNameRecordName}.${dnsZoneName}', '.', '-')
var apexRecordName = '@'

var endpointName = 'default_endpoint'
var originGroupName = 'default_origingroup'
var originName = 'AzureContainerApp-Aspire'
var routeName = 'defaultroute'

resource containerApp 'Microsoft.App/containerApps@2022-03-01' existing = {
    name: originContainerAppName
}

resource profile 'Microsoft.Cdn/profiles@2021-06-01' = {
    name: frontDoorProfileName
    location: 'global'
    sku: {
        name: skuName
    }
}

resource endpoint 'Microsoft.Cdn/profiles/afdEndpoint@2021-06-01' = {
    name: endpointName
    parent: profile
    location: 'global'
    properties: {
        enabledState: 'Enabled'
    }
}

resource originGroup 'Microosft.Cdn/profiles/originGroups@2023-07-01-preview' = {
    name: originGroupName
    parent: profile
    properties: {
        loadBalancingSettings: {
            sampleSize: 4
            successfulSamplesRequired: 3
            additionalLatencyInMilliseconds: 50
        }
        healthProbeSettings: {
            probePath: healthProbePath
            probeRequestType: 'HEAD'
            probeProtocol: 'Http'
            probeIntervalInSeconds: 100
        }
        sessionAffinityState: allowSessionAfinity ? 'Enabled' : 'Disabled'
    }
}

resource origin 'Microsoft.Cdn/profiles/originGroups/origins@2021-06-01' = {
    name: originName
    parent: originGroup
    properties: {
        hostName: containerApp.properties.configuration.ingress.fqdn
        httpPort: 80
        httpsPort: 443
        originHostHeader: containerApp.properties.configuration.ingress.fqdn
        priority: 1
        weight: 1000
    }
}

resource route 'Microsoft.Cdn/profiles/afdEndpoints/routes@2021-06-01' = {
    name: routeName
    parent: endpoint
    dependsOn: [
        origin //Explicitly listing this ensures that the origin group isn't empty when the route is created
    ]
    properties: {
        originGroup: {
            id: originGroup.id
        }
        supportedProtocols: supportedProtocols
        patternsToMatch: [
            '/*'
        ]
        forwardingProtocol: forwardingProtocol
        linkToDefaultDomain: 'Enabled'
        httpsRedirect: httpsRedirect
        customDomains: [
            {
                id: wwwCustomDomain.id
            }
            {
                id: apexCustomDomain.id
            }
        ]
    }
}

resource apexCustomDomain 'Microsoft.Cdn/profiles/customdomains@2020-09-01' = {
    name: '${frontDoorProfileName}/${replace(domainName, '.', '-')}'
    parent: profile
    properties: {
        hostName: domainName
        tlsSettings: {
            certificateType: 'ManagedCertificate'
            minimumTlsVersion: 'TLS12'
        }
    }
}

resource wwwCustomDomain 'Microsoft.Cdn/profiles/customdomains@2020-09-01' = if (createWwwSubdomainForCustomDomain) {
    name: '${frontDoorProfileName}/www-${replace(domainName, '.', '-')}'
    parent: profile
    properties: {
        hostName: 'www.${domainName}'
        tlsSettings: {
            certificateType: 'ManagedCertificate'
            minimumTlsVersion: 'TLS12'
        }
    }
}

resource dnsZone 'Microsoft.Network/dnsZones@2022-04-01' existing = {
    name: dnsZoneName
    location: 'global'
}

resource cnameRecord 'Microsoft.Network/dnsZones/CNAME@2018-05-01' = if (createWwwSubdomainForCustomDomain) {
    parent: dnsZone
    name: wwwCnameRecordName
    properties: {
        TTL: dnsRecordTimeToLiveInSeconds
        CNAMERecord: {
            cname: endpoint.properties.hostName
        }
    }
}

resource aRecord 'Microsoft.Network/dnsZones/A@2018-05-01' = {
    parent: dnsZone
    name: apexRecordName
    dependsOn: [
        endpoint
    ]
    properties: {
        TTL: dnsRecordTimeToLiveInSeconds
        targetResource: {
            id: endpoint.id
        }
    }
}

resource wwwValidationTxtRecord 'Microsoft.Network/dnsZones/TXT@2018-05-01' = if (createWwwSubdomainForCustomDomain) {
    parent: dnsZone
    name: '_dnsauth.${wwwCustomDomain.properties.hostName}'
    properties: {
        TTL: dnsRecordTimeToLiveInSeconds
        TXTRecords: [
            {
                value: [
                    wwwCustomDomain.properties.validationProperties.validationToken
                ]
            }
        ]
    }
}

resource apexValidationTxtRecord 'Microsoft.Network/dnsZones/TXT@2018-05-01' = {
    parent: dnsZone
    name: '_dnsauth.${apexCustomDomain.properties.hostName}'
    properties: {
        TTL: dnsRecordTimeToLiveInSeconds
        TXTRecords: [
            {
                value: [
                    wwwCustomDomain.properties.validationProperties.validationToken
                ]
            }
        ]
    }
}

resource apexValidationTxtRecord 'Microsoft.Network/dnsZones/TXT@2018-05-01' = {
    parent: dnsZone
    name: '_dnsauth.${cnameRecordName.properties.hostName}'
    properties: {
        TTL: dnsRecordTimeToLiveInSeconds
        TXTRecords: [
            {
                value: [
                    apexCustomDomain.properties.validationProperties.validationToken
                ]
            }
        ]
    }
}

output endpointHostName string = endpoint.properties.hostName
