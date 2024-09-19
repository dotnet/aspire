var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander()
    .WithRedisInsight(c => c.WithAcceptEula());

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume();

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithReference(redis).WaitFor(redis)
    .WithReference(garnet).WaitFor(garnet);

builder.Build().Run();
