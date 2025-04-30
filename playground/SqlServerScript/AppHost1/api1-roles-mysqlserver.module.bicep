@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param mysqlserver_outputs_name string

param principalName string

param principalId string

param mysqlserver_outputs_sqlserveradminname string

resource mysqlserver 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: mysqlserver_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: mysqlserver_outputs_sqlserveradminname
}

resource script_81de323e2cb9c0ad 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: take('scriptdeecbcad${uniqueString(resourceGroup().id)}', 24)
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${sqlServerAdmin.id}': { }
    }
  }
  kind: 'AzurePowerShell'
  properties: {
    scriptContent: '\$sqlServerFqdn = "\$env:DBSERVER"\r\n\$sqlDatabaseName = "\$env:DBNAME"\r\n\$username = "\$env:USERNAME"\r\n\$clientId = "\$env:CLIENTID"\r\n\r\n# Install SqlServer module\r\nInstall-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser\r\nImport-Module SqlServer\r\n\r\n\$sqlCmd = @"\r\nDECLARE @principal_name SYSNAME = \'\$username\';\r\nDECLARE @clientId UNIQUEIDENTIFIER = \'\$clientId\';\r\n\r\n-- Convert the guid to the right type\r\nDECLARE @castClientId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @clientId), 1);\r\n\r\n-- Construct command: CREATE USER [@principal_name] WITH SID = @castClientId, TYPE = E;\r\nDECLARE @cmd NVARCHAR(MAX) = N\'CREATE USER [\' + @principal_name + \'] WITH SID = \' + @castClientId + \', TYPE = E;\'\r\nEXEC (@cmd);\r\n\r\n-- Assign roles to the new user\r\nDECLARE @role1 NVARCHAR(MAX) = N\'ALTER ROLE db_datareader ADD MEMBER [\' + @principal_name + \']\';\r\nEXEC (@role1);\r\n\r\nDECLARE @role2 NVARCHAR(MAX) = N\'ALTER ROLE db_datawriter ADD MEMBER [\' + @principal_name + \']\';\r\nEXEC (@role2);\r\n"@\r\n# Note: the string terminator must not have whitespace before it, therefore it is not indented.\r\n\r\nWrite-Host \$sqlCmd\r\n\r\n\$connectionString = "Server=tcp:\${sqlServerFqdn},1433;Initial Catalog=\${sqlDatabaseName};Authentication=Active Directory Default;"\r\n\r\nInvoke-Sqlcmd -ConnectionString \$connectionString -Query \$sqlCmd'
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
        name: 'USERNAME'
        value: principalName
      }
      {
        name: 'CLIENTID'
        value: principalId
      }
    ]
  }
}