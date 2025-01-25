var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");
var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator().WithHub("myhub");
#if !SKIP_UNSTABLE_EMULATORS
var serviceBus = builder.AddAzureServiceBus("messaging").RunAsEmulator().WithQueue("myqueue");
#endif
#pragma warning disable ASPIRECOSMOS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator(e => e.WithDataExplorer())
    .WithDatabase("mydatabase", (database)
        => database.Containers.AddRange([new("mycontainer", "/id"), new("leases", "/id")]))
    .WithAccessKeyAuthentication();
#pragma warning restore ASPIRECOSMOS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithExternalHttpEndpoints()
    .WithReference(eventHubs).WaitFor(eventHubs)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
#endif
    .WithReference(cosmosDb)
    .WithReference(blob)
    .WithReference(queue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(eventHubs).WaitFor(eventHubs)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
#endif
    .WithReference(cosmosDb)
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(funcApp);

builder.Build().Run();
