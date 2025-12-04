var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var server = builder.AddProject<Projects.GeneratedClassNamePrefix_Server>("server")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithHttpHealthCheck("/health");

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server)
    .WithExternalHttpEndpoints();

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
