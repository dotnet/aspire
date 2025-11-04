#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.NodeJs@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Python@!!REPLACE_WITH_LATEST_VERSION!!
#if UseRedisCache
#:package Aspire.Hosting.Redis@!!REPLACE_WITH_LATEST_VERSION!!
#endif

var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var app = builder.AddUvicornApp("app", "./app", "main:app")
    .WithUv()
    .WithExternalHttpEndpoints()
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithHttpHealthCheck("/health");

var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithReference(app)
    .WaitFor(app);

app.PublishWithContainerFiles(frontend, "./static");

builder.Build().Run();
