var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var apiService = builder.AddProject<Projects.AspireStarterApplication__1_ApiService>("apiservice");

builder.AddProject<Projects.AspireStarterApplication__1_Web>("webfrontend")
#if UseRedisCache
    .WithReference(cache)
#endif
    .WithReference(apiService);

builder.Build().Run();
