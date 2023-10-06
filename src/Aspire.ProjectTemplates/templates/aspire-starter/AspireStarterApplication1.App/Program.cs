#if UseRedisCache
using Aspire.Hosting.Redis;
#endif
using Projects = AspireStarterApplication1.App.Projects;

var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedisContainer("cache");

#endif
var apiservice = builder.AddProject<Projects.AspireStarterApplication1_ApiService>("apiservice");

builder.AddProject<Projects.AspireStarterApplication1_Web>("webfrontend")
#if UseRedisCache
    .WithRedis(cache)
#endif
    .WithServiceReference(apiservice);

builder.Build().Run();
