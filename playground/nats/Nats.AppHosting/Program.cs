using Aspire.Hosting.Nats;

var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats");

builder.AddProject<Projects.Nats_ApiService>("apiservice")
    .WithReference(nats);

builder.Build().Run();
