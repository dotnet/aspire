extension 'br:mcr.microsoft.com/bicep/extensions/microsoftgraph/v1.0:1.0.0' as microsoftGraphV1

@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationName string

param principalId string

@allowed([
  'AzureADMyOrg'
  'AzureADMultipleOrgs'
  'AzureADandPersonalMicrosoftAccount'
  'PersonalMicrosoftAccount'
])
param signInAudience string = 'AzureADMyOrg'

param redirectUris array = []

param enableIdTokenIssuance bool = false

resource application 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'Contoso\'s web application'
  uniqueName: '${applicationName}-${uniqueString(resourceGroup().id)}'
  signInAudience: signInAudience
  api: {
    requestedAccessTokenVersion: 2
  }
  web: {
    redirectUris: [
      'https://localhost:7123/signin-oidc'
      'https://localhost:7123/auth/callback'
    ]
    implicitGrantSettings: {
      enableIdTokenIssuance: true
    }
  }
  owners: {
    relationships: [
      principalId
    ]
  }
}

resource servicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: application.appId
  owners: {
    relationships: [
      principalId
    ]
  }
}

output clientId string = application.appId

output tenantId string = tenant().tenantId

output objectId string = application.id

output servicePrincipalId string = servicePrincipal.id

output uniqueName string = application.uniqueName

output applicationDisplayName string = application.displayName