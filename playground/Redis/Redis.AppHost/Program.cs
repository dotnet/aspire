var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander()
    .WithRedisInsight(c => c.WithAcceptEula(true));

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume("garnet-data");

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithReference(redis).WaitFor(redis)
    .WithReference(garnet).WaitFor(garnet);

builder.Build().Run();
