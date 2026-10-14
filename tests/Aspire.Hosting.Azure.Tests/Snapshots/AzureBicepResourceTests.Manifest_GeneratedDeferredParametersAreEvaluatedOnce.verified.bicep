@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param enabled bool

@secure()
param secret string