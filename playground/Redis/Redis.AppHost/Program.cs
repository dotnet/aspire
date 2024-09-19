var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander()
    .WithRedisInsight(c => c.WithAcceptEula(true));

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
