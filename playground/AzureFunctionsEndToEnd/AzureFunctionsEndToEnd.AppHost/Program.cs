var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");
var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator().WithHub("myhub");
var serviceBus = builder.AddAzureServiceBus("messaging").RunAsEmulator().WithQueue("myqueue");
#if !SKIP_UNSTABLE_EMULATORS
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator()
    .WithDatabase("mydatabase", (database)
        => database.Containers.AddRange([new("mycontainer", "/id")]));
#endif

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithExternalHttpEndpoints()
    .WithReference(eventHubs).WaitFor(eventHubs)
    .WithReference(serviceBus).WaitFor(serviceBus)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(blob)
    .WithReference(queue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(eventHubs).WaitFor(eventHubs)
    .WithReference(serviceBus).WaitFor(serviceBus)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(funcApp);

builder.Build().Run();
