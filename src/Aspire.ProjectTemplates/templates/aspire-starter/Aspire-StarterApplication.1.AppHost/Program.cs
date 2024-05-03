var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var apiService = builder.AddProject<Projects.GeneratedClassNamePrefix_ApiService>("apiservice");

builder.AddProject<Projects.GeneratedClassNamePrefix_Web>("webfrontend")
    .WithExternalHttpEndpoints()
#if UseRedisCache
    .WithReference(cache)
#endif
    .WithReference(apiService);

builder.Build().Run();
