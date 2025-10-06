﻿@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existing_kusto_name string

param existing_kusto_rg string

resource test_kusto 'Microsoft.Kusto/clusters@2024-04-13' existing = {
  name: existing_kusto_name
  scope: resourceGroup(existing_kusto_rg)
}