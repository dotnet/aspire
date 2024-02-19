param test string
param values array
@description('The location used for all deployed resources')
param location string = resourceGroup().location

output test string = test
output val0 string = values[0]
output val1 string = values[1]
