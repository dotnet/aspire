var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander();

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume("garnet-data");

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithReference(redis).WaitFor(redis)
    .WithReference(garnet).WaitFor(garnet);

builder.Build().Run();
