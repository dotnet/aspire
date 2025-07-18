var builder = DistributedApplication.CreateBuilder(args);

// required for the event processor client which will use the connectionName to get the connectionString.
var storage = builder.AddAzureStorage("ehstorage")
    .RunAsEmulator();
var blob = storage.GetBlobService();

var eventHub = builder.AddAzureEventHubs("eventhubns")
    .RunAsEmulator()
    .AddHub("eventhubOne", "eventhub");

builder.AddProject<Projects.EventHubsConsumer>("consumer")
    .WithReference(eventHub).WaitFor(eventHub)
    .WithReference(blob, "checkpoints");

builder.AddProject<Projects.EventHubsApi>("api")
    .WithExternalHttpEndpoints()
    .WithReference(eventHub).WaitFor(eventHub);

builder.Build().Run();
