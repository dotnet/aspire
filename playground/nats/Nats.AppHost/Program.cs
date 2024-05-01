var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats")
    .WithJetStream();

builder.AddProject<Projects.Nats__ApiService>("api")
    .WithExternalHttpEndpoints()
    .WithReference(nats);

builder.AddProject<Projects.Nats__Backend>("backend")
    .WithReference(nats);

builder.Build().Run();
