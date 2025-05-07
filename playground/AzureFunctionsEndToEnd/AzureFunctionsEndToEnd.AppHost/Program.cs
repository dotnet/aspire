var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var queues = storage.AddQueues("queues");
var myQueue = queues.AddQueue("myqueue1");

var blobs = storage.AddBlobs("blobs");
var myBlobContainer = blobs.AddBlobContainer("myblobcontainer");

var eventHub = builder.AddAzureEventHubs("eventhubs")
    .RunAsEmulator()
    .AddHub("myhub");
#if !SKIP_UNSTABLE_EMULATORS
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();
serviceBus.AddServiceBusQueue("myqueue");
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
    .WithReference(blobs)
    .WithReference(myBlobContainer).WaitFor(myBlobContainer)
    .WithReference(queues)
    .WithReference(myQueue).WaitFor(myQueue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(eventHub).WaitFor(eventHub)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(queues)
    .WithReference(blobs)
    .WithReference(funcApp);

builder.Build().Run();
