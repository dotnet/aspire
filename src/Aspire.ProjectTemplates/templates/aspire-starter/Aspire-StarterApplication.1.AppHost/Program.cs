var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var apiService = builder.AddProject<Projects.Aspire_StarterApplication__1_ApiService>("apiservice");

builder.AddProject<Projects.Aspire_StarterApplication__1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
#if UseRedisCache
    .WithReference(cache)
#endif
    .WithReference(apiService);

builder.Build().Run();
