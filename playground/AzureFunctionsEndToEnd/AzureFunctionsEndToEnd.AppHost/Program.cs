var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");
var eventHub = builder.AddAzureEventHubs("eventhubs")
    .RunAsEmulator()
    .AddHub("myhub");
#if !SKIP_UNSTABLE_EMULATORS
var serviceBus = builder.AddAzureServiceBus("messaging").RunAsEmulator().WithQueue("myqueue");
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();
var database = cosmosDb.AddCosmosDatabase("mydatabase");
database.AddContainer("mycontainer", "/id");

#endif

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithExternalHttpEndpoints()
    .WithReference(eventHub).WaitFor(eventHub)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(blob)
    .WithReference(queue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(eventHub).WaitFor(eventHub)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(funcApp);

builder.Build().Run();
