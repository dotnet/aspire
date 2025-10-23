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
var apiService = builder.AddPythonScript("app", "./app", "app.py")
    .WithUvEnvironment()
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .PublishAsDockerFile(c =>
    {
        c.WithDockerfile(".");
    });

builder.AddViteApp("frontend", "./frontend")
    .WithNpm()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
