var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var backend = builder.AddProject<Projects.GeneratedClassNamePrefix_Backend>("backend")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithHttpHealthCheck("/health");

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(backend)
    .WaitFor(backend)
    .WithExternalHttpEndpoints();

builder.Build().Run();
