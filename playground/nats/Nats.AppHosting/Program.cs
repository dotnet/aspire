var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats")
    .WithJetStream()
    .PublishAsContainer();

builder.AddProject<Projects.Nats_ApiService>("apiservice")
    .WithReference(nats);

builder.Build().Run();
