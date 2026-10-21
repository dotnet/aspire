@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param api_host string

param env_environment_id string

resource frontdoor 'Microsoft.Cdn/profiles@2025-06-01' = {
  name: take('frontdoor-${uniqueString(resourceGroup().id)}', 260)
  tags: {
    'aspire-resource-name': 'frontdoor'
  }
  location: 'Global'
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
}

resource apiEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2025-06-01' = {
  name: take('apiEndpoint-${uniqueString(resourceGroup().id)}', 46)
  location: 'Global'
  parent: frontdoor
}

resource apiOriginGroup 'Microsoft.Cdn/profiles/originGroups@2025-06-01' = {
  name: take('apiOriginGroup-${uniqueString(resourceGroup().id)}', 90)
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
    healthProbeSettings: {
      probePath: '/'
      probeProtocol: 'Https'
    }
  }
  parent: frontdoor
}

resource apiOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2025-06-01' = {
  name: take('apiOrigin-${uniqueString(resourceGroup().id, api_host)}', 90)
  properties: {
    hostName: api_host
    originHostHeader: api_host
    sharedPrivateLinkResource: {
      privateLink: {
        id: env_environment_id
      }
      privateLinkLocation: 'westus2'
      groupId: 'managedEnvironments'
      requestMessage: 'Azure Front Door frontdoor requests access to env.'
    }
    enforceCertificateNameCheck: true
  }
  parent: apiOriginGroup
}

resource apiRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2025-06-01' = {
  name: take('apiRoute-${uniqueString(resourceGroup().id)}', 90)
  properties: {
    originGroup: {
      id: apiOriginGroup.id
    }
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
  }
  parent: apiEndpoint
  dependsOn: [
    apiOrigin
  ]
}

output api_endpointUrl string = 'https://${apiEndpoint.properties.hostName}'

output api_endpointId string = apiEndpoint.id