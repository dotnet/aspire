var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander()
    .WithRedisInsight(c => c.WithAcceptEula());

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume();

var valkey = builder.AddValkey("valkey")
    .WithDataVolume("valkey-data");

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithReference(redis).WaitFor(redis)
    .WithReference(garnet).WaitFor(garnet)
    .WithReference(valkey).WaitFor(valkey);

builder.Build().Run();
