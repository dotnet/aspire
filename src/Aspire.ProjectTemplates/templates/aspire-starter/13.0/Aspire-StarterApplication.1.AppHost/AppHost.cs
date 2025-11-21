var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var apiService = builder.AddProject<Projects.GeneratedClassNamePrefix_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

#if (FrontendType == "JavaScript")
var frontend = builder.AddViteApp("webfrontend", "../XmlEncodedProjectName.Frontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithReference(apiService)
    .WaitFor(apiService);
#else
builder.AddProject<Projects.GeneratedClassNamePrefix_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

builder.Build().Run();
