using Aspire.Hosting.Nats;

var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats", enableJetStream: true);

builder.AddProject<Projects.Nats_ApiService>("apiservice")
    .WithReference(nats);

builder.Build().Run();
