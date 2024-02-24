var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats")
    .WithJetStream();

builder.AddProject<Projects.Nats_ApiService>("api")
    .WithReference(nats);

builder.AddProject<Projects.Nats_Backend>("backend")
    .WithReference(nats);

builder.Build().Run();
