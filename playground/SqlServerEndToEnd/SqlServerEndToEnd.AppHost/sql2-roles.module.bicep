@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sql2_outputs_name string

param sql2_outputs_sqlserveradminname string

param principalId string

param principalName string

param principalType string

resource sql2 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: sql2_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: sql2_outputs_sqlserveradminname
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: principalName
}

resource script_sql2_db2 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: take('script-${uniqueString('sql2', principalName, 'db2', resourceGroup().id)}', 24)
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${sqlServerAdmin.id}': { }
    }
  }
  kind: 'AzurePowerShell'
  properties: {
    scriptContent: '\$sqlServerFqdn = "\$env:DBSERVER"\r\n\$sqlDatabaseName = "\$env:DBNAME"\r\n\$principalName = "\$env:PRINCIPALNAME"\r\n\$id = "\$env:ID"\r\n\r\n# Install SqlServer module\r\nInstall-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser\r\nImport-Module SqlServer\r\n\r\n\$sqlCmd = @"\r\nDECLARE @name SYSNAME = \'\$principalName\';\r\nDECLARE @id UNIQUEIDENTIFIER = \'\$id\';\r\n\r\n-- Convert the guid to the right type\r\nDECLARE @castId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @id), 1);\r\n\r\n-- Construct command: CREATE USER [@name] WITH SID = @castId, TYPE = E;\r\nDECLARE @cmd NVARCHAR(MAX) = N\'CREATE USER [\' + @name + \'] WITH SID = \' + @castId + \', TYPE = E;\'\r\nEXEC (@cmd);\r\n\r\n-- Assign roles to the new user\r\nDECLARE @role1 NVARCHAR(MAX) = N\'ALTER ROLE db_owner ADD MEMBER [\' + @name + \']\';\r\nEXEC (@role1);\r\n\r\n"@\r\n# Note: the string terminator must not have whitespace before it, therefore it is not indented.\r\n\r\nWrite-Host \$sqlCmd\r\n\r\n\$connectionString = "Server=tcp:\${sqlServerFqdn},1433;Initial Catalog=\${sqlDatabaseName};Authentication=Active Directory Default;"\r\n\r\nInvoke-Sqlcmd -ConnectionString \$connectionString -Query \$sqlCmd'
    azPowerShellVersion: '10.0'
    retentionInterval: 'PT1H'
    environmentVariables: [
      {
        name: 'DBNAME'
        value: 'db2'
      }
      {
        name: 'DBSERVER'
        value: sql2.properties.fullyQualifiedDomainName
      }
      {
        name: 'PRINCIPALTYPE'
        value: principalType
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
  }
}