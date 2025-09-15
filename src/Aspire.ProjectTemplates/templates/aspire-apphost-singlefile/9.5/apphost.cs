#:sdk Microsoft.NET.Sdk
#:sdk Aspire.AppHost.Sdk@aspireVersion
#:package Aspire.Hosting.AppHost@aspireVersion
#:property PublishAot=false

var builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();
