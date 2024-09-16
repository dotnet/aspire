var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander();

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
