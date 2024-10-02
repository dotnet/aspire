var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");
var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator().AddEventHub("myhub");

#if !SKIP_PROVISIONED_AZURE_RESOURCE
var serviceBus = builder.AddAzureServiceBus("messaging").AddQueue("myqueue");
#endif

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithExternalHttpEndpoints()
    .WithReference(eventHubs)
#if !SKIP_PROVISIONED_AZURE_RESOURCE
    .WithReference(serviceBus)
#endif
    .WithReference(blob)
    .WithReference(queue);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(eventHubs)
#if !SKIP_PROVISIONED_AZURE_RESOURCE
    .WithReference(serviceBus)
#endif
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(funcApp);

builder.Build().Run();
