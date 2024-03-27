var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

// required for the event processor client which will use the connectionName to get the connectionString.
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
