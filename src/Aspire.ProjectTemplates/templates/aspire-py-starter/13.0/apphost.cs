#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.NodeJs@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Python@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Redis@!!REPLACE_WITH_LATEST_VERSION!!
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.8.0

#pragma warning disable ASPIREHOSTINGPYTHON001

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddPythonScript("apiservice", "./api_service", "app.py")
    .WithUvEnvironment()
    .WithReference(cache)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(c =>
    {
        c.WithDockerfile(".");
    });

builder.AddViteApp("frontend", "./frontend")
    .WithNpmPackageInstallation()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
