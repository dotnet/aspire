var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var blob = builder.AddAzureStorage("ehstorage")
    .AddBlobs("checkpoints");

var eventHub = builder.AddAzureEventHubs("eventhubns")
    .AddEventHub("hub");

builder.AddProject<Projects.EventHubsConsumer>("consumer")
    .WithReference(eventHub)
    .WithReference(blob);

builder.AddProject<Projects.EventHubsApi>("api")
    .WithReference(eventHub);

builder.Build().Run();
