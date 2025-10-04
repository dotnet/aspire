@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param kusto_outputs_name string

param principalId string

resource kusto 'Microsoft.Kusto/clusters@2024-04-13' existing = {
  name: kusto_outputs_name
}

resource testdb 'Microsoft.Kusto/clusters/databases@2024-04-13' existing = {
  name: 'testdb'
  parent: kusto
}

resource testdb_user 'Microsoft.Kusto/clusters/databases/principalAssignments@2024-04-13' = {
  name: guid(testdb.id, principalId, 'User')
  properties: {
    principalId: principalId
    principalType: 'App'
    role: 'User'
  }
  parent: testdb
}