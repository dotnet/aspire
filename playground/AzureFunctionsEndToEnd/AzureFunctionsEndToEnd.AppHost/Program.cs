var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueue("queue");
var blobContainer = storage.AddBlobContainer("myblobcontainer");

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
    .WithReference(blobContainer).WaitFor(blobContainer)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(blobContainer).WaitFor(blobContainer)
    .WithReference(queue).WaitFor(queue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(eventHub).WaitFor(eventHub)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(queue).WaitFor(queue)
    .WithReference(blobContainer).WaitFor(blobContainer)
    .WithReference(funcApp);

builder.Build().Run();
