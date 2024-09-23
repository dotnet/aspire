using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");

#if !SKIP_EVENTHUBS_EMULATION
var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator().AddEventHub("myhub");
var serviceBus = builder.AddAzureServiceBus("messaging").AddQueue("myqueue");
#endif

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithExternalHttpEndpoints()
#if !SKIP_EVENTHUBS_EMULATION
    .WithReference(eventHubs)
    .WithReference(serviceBus)
#endif
    .WithReference(blob)
    .WithReference(queue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
#if !SKIP_EVENTHUBS_EMULATION
    .WithReference(eventHubs)
    .WithReference(serviceBus)
#endif
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(funcApp);

builder.Build().Run();
