#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.NodeJs@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Python@!!REPLACE_WITH_LATEST_VERSION!!
#if UseRedisCache
#:package Aspire.Hosting.Redis@!!REPLACE_WITH_LATEST_VERSION!!
#endif

#pragma warning disable ASPIREHOSTINGPYTHON001

var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var app = builder.AddUvicornApp("app", "./app", "app:app")
    .WithUvEnvironment()
    .WithExternalHttpEndpoints()
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithHttpHealthCheck("/health");

builder.AddViteApp("frontend", "./frontend")
    .WithNpmPackageManager()
    .WithReference(apiService)
    .WaitFor(apiService);

app.PublishWithContainerFiles(frontend, "./static");

builder.Build().Run();