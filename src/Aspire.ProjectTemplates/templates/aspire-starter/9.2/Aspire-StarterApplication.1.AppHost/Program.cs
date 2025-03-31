var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var apiService = builder.AddProject<Projects.GeneratedClassNamePrefix_ApiService>("apiservice")
#if HasHttpsProfile
    .WithHttpsHealthCheck("/health");
#else
    .WithHttpHealthCheck("/health");
#endif

builder.AddProject<Projects.GeneratedClassNamePrefix_Web>("webfrontend")
    .WithExternalHttpEndpoints()
#if HasHttpsProfile
    .WithHttpsHealthCheck("/health")
#else
    .WithHttpHealthCheck("/health")
#endif
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
