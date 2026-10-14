@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param enabled bool

param count int

param redirectUris array

param options object

param jsonArray array

param jsonBoolean bool

param jsonInteger int

@secure()
param secret string