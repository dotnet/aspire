var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var blob = builder.AddAzureStorage("ehstorage")
    .AddBlobs("checkpoints");

var eventHub = builder.AddAzureEventHubs("eventhubns")
    .AddHub("hub");

builder.AddProject<Projects.EventHubsConsumer>("consumer")
    .WithReference(eventHub)
    .WithReference(blob);

builder.AddProject<Projects.EventHubsApi>("api")
    .WithReference(eventHub);

builder.Build().Run();
