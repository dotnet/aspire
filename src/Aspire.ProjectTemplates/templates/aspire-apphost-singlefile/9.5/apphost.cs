#:sdk Microsoft.NET.Sdk
#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.AppHost@!!REPLACE_WITH_LATEST_VERSION!!
#:property PublishAot=false

var builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();
