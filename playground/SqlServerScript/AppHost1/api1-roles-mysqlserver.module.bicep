@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param mysqlserver_outputs_name string

param mysqlserver_outputs_sqlserveradminname string

param principalId string

param principalName string

resource mysqlserver 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: mysqlserver_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: mysqlserver_outputs_sqlserveradminname
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: principalName
}

resource script_mysqlserver_todosdb 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: take('script-${uniqueString('mysqlserver', principalName, 'todosdb', resourceGroup().id)}', 24)
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${sqlServerAdmin.id}': { }
    }
  }
  kind: 'AzurePowerShell'
  properties: {
    scriptContent: '\$sqlServerFqdn = "\$env:DBSERVER"\n\$sqlDatabaseName = "\$env:DBNAME"\n\$principalName = "\$env:PRINCIPALNAME"\n\$id = "\$env:ID"\n\n# Install SqlServer module\nInstall-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser\nImport-Module SqlServer\n\n\$sqlCmd = @"\nDECLARE @name SYSNAME = \'\$principalName\';\nDECLARE @id UNIQUEIDENTIFIER = \'\$id\';\n\n-- Convert the guid to the right type\nDECLARE @castId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @id), 1);\n\n-- Construct command: CREATE USER [@name] WITH SID = @castId, TYPE = E;\nDECLARE @cmd NVARCHAR(MAX) = N\'CREATE USER [\' + @name + \'] WITH SID = \' + @castId + \', TYPE = E;\'\nEXEC (@cmd);\n\n-- Assign roles to the new user\nDECLARE @role1 NVARCHAR(MAX) = N\'ALTER ROLE db_owner ADD MEMBER [\' + @name + \']\';\nEXEC (@role1);\n\n"@\n# Note: the string terminator must not have whitespace before it, therefore it is not indented.\n\nWrite-Host \$sqlCmd\n\n\$connectionString = "Server=tcp:\${sqlServerFqdn},1433;Initial Catalog=\${sqlDatabaseName};Authentication=Active Directory Default;"\n\nInvoke-Sqlcmd -ConnectionString \$connectionString -Query \$sqlCmd'
    azPowerShellVersion: '7.4'
    retentionInterval: 'PT1H'
    environmentVariables: [
      {
        name: 'DBNAME'
        value: 'todosdb'
      }
      {
        name: 'DBSERVER'
        value: mysqlserver.properties.fullyQualifiedDomainName
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
  }
}