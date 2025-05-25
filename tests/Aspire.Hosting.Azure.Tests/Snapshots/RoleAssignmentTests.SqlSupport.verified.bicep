@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sql_outputs_name string

param sql_outputs_sqlserveradminname string

param principalId string

param principalName string

resource sql 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: sql_outputs_name
}

resource sqlServerAdmin 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: sql_outputs_sqlserveradminname
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: principalName
}

output name string = mi.name