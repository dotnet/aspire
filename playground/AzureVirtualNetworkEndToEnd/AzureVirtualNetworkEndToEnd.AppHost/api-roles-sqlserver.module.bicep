@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sqlserver_outputs_name string

param sqlserver_outputs_sqlserveradminname string

param vnet_outputs_name string

param vnet_outputs_private_endpoints_id string

param principalId string

param principalName string

resource sqlserver 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: sqlserver_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: sqlserver_outputs_sqlserveradminname
}

resource existingVnet 'Microsoft.Network/virtualNetworks@2025-05-01' existing = {
  name: vnet_outputs_name
}

resource aciSubnetNsg 'Microsoft.Network/networkSecurityGroups@2025-05-01' = {
  name: take('aciSubnetNsg-${uniqueString(resourceGroup().id)}', 80)
  location: location
  tags: {
    'aspire-resource-name': 'aci-subnet-nsg'
  }
}

resource allow_outbound_443_AzureActiveDirectory 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'allow-outbound-443-AzureActiveDirectory'
  properties: {
    access: 'Allow'
    destinationAddressPrefix: 'AzureActiveDirectory'
    destinationPortRange: '443'
    direction: 'Outbound'
    priority: 100
    protocol: '*'
    sourceAddressPrefix: '*'
    sourcePortRange: '*'
  }
  parent: aciSubnetNsg
}

resource allow_outbound_443_Sql 'Microsoft.Network/networkSecurityGroups/securityRules@2025-05-01' = {
  name: 'allow-outbound-443-Sql'
  properties: {
    access: 'Allow'
    destinationAddressPrefix: 'Sql'
    destinationPortRange: '443'
    direction: 'Outbound'
    priority: 200
    protocol: '*'
    sourceAddressPrefix: '*'
    sourcePortRange: '*'
  }
  parent: aciSubnetNsg
}

resource aciSubnet 'Microsoft.Network/virtualNetworks/subnets@2025-05-01' = {
  name: 'aci-deployment-script-subnet'
  properties: {
    addressPrefix: '10.0.255.248/29'
    delegations: [
      {
        properties: {
          serviceName: 'Microsoft.ContainerInstance/containerGroups'
        }
        name: 'Microsoft.ContainerInstance/containerGroups'
      }
    ]
    networkSecurityGroup: {
      id: aciSubnetNsg.id
    }
  }
  parent: existingVnet
}

resource depScriptStorage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('depscriptstorage${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Deny'
    }
    publicNetworkAccess: 'Disabled'
  }
  tags: {
    'aspire-resource-name': 'dep-script-storage'
  }
}

resource depScriptStorage_sqlServerAdmin_StorageFileDataPrivilegedContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(depScriptStorage.id, sqlServerAdmin.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69566ab7-960f-475b-8e7c-b3118f30c6bd'))
  properties: {
    principalId: sqlServerAdmin.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69566ab7-960f-475b-8e7c-b3118f30c6bd')
    principalType: 'ServicePrincipal'
  }
  scope: depScriptStorage
}

resource depStorageFilesPe 'Microsoft.Network/privateEndpoints@2025-05-01' = {
  name: take('depStorageFilesPe-${uniqueString(resourceGroup().id)}', 64)
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        properties: {
          privateLinkServiceId: depScriptStorage.id
          groupIds: [
            'file'
          ]
        }
        name: 'dep-storage-files-connection'
      }
    ]
    subnet: {
      id: vnet_outputs_private_endpoints_id
    }
  }
  tags: {
    'aspire-resource-name': 'dep-storage-files-pe'
  }
}

resource depStorageFilesDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.file.core.windows.net'
  location: 'global'
}

resource depStorageFilesDnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  name: 'dep-storage-files-vnet-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: existingVnet.id
    }
  }
  parent: depStorageFilesDnsZone
}

resource depStorageFilesPe_dnsgroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2025-05-01' = {
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'depStorageFilesDnsZone'
        properties: {
          privateDnsZoneId: depStorageFilesDnsZone.id
        }
      }
    ]
  }
  parent: depStorageFilesPe
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: principalName
}

resource script_sqlserver_sqldb 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: take('script-${uniqueString('sqlserver', principalName, 'sqldb', resourceGroup().id)}', 24)
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${sqlServerAdmin.id}': { }
    }
  }
  kind: 'AzurePowerShell'
  properties: {
    azPowerShellVersion: '14.0'
    retentionInterval: 'PT1H'
    containerSettings: {
      subnetIds: [
        {
          id: aciSubnet.id
        }
      ]
    }
    environmentVariables: [
      {
        name: 'DBNAME'
        value: 'sqldb'
      }
      {
        name: 'DBSERVER'
        value: sqlserver.properties.fullyQualifiedDomainName
      }
      {
        name: 'PRINCIPALTYPE'
        value: 'ServicePrincipal'
      }
      {
        name: 'PRINCIPALNAME'
        value: principalName
      }
      {
        name: 'ID'
        value: mi.properties.clientId
      }
    ]
    scriptContent: '\$sqlServerFqdn = "\$env:DBSERVER"\r\n\$sqlDatabaseName = "\$env:DBNAME"\r\n\$principalName = "\$env:PRINCIPALNAME"\r\n\$id = "\$env:ID"\r\n\r\n# Install SqlServer module - using specific version to avoid breaking changes in 22.4.5.1 (see https://github.com/dotnet/aspire/issues/9926)\r\nInstall-Module -Name SqlServer -RequiredVersion 22.3.0 -Force -AllowClobber -Scope CurrentUser\r\nImport-Module SqlServer\r\n\r\n\$sqlCmd = @"\r\nDECLARE @name SYSNAME = \'\$principalName\';\r\nDECLARE @id UNIQUEIDENTIFIER = \'\$id\';\r\n\r\n-- Convert the guid to the right type\r\nDECLARE @castId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @id), 1);\r\n\r\n-- Construct command: CREATE USER [@name] WITH SID = @castId, TYPE = E;\r\nDECLARE @cmd NVARCHAR(MAX) = N\'CREATE USER [\' + @name + \'] WITH SID = \' + @castId + \', TYPE = E;\'\r\nEXEC (@cmd);\r\n\r\n-- Assign roles to the new user\r\nDECLARE @role1 NVARCHAR(MAX) = N\'ALTER ROLE db_owner ADD MEMBER [\' + @name + \']\';\r\nEXEC (@role1);\r\n\r\n"@\r\n# Note: the string terminator must not have whitespace before it, therefore it is not indented.\r\n\r\nWrite-Host \$sqlCmd\r\n\r\n\$connectionString = "Server=tcp:\${sqlServerFqdn},1433;Initial Catalog=\${sqlDatabaseName};Authentication=Active Directory Default;"\r\n\r\nInvoke-Sqlcmd -ConnectionString \$connectionString -Query \$sqlCmd'
    storageAccountSettings: {
      storageAccountName: depScriptStorage.name
    }
  }
}