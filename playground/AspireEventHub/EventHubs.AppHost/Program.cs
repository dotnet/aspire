#define EMULATOR

var builder = DistributedApplication.CreateBuilder(args);

// required for the event processor client which will use the connectionName to get the connectionString.
var blob = builder.AddAzureStorage("ehstorage")
#if EMULATOR
    .RunAsEmulator()
#endif
    .AddBlobs("checkpoints");

var eventHub = builder.AddAzureEventHubs("eventhubns")
#if EMULATOR
    .RunAsEmulator()
#endif
    .WithHub("hub")
    .WithHub("hub2")
    .WithDefaultEntity("hub");

builder.AddProject<Projects.EventHubsConsumer>("consumer")
    .WithReference(eventHub).WaitFor(eventHub)
    .WithReference(blob);

builder.AddProject<Projects.EventHubsApi>("api")
    .WithExternalHttpEndpoints()
    .WithReference(eventHub).WaitFor(eventHub);

builder.Build().Run();
