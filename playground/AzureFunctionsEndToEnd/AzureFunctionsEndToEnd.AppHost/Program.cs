using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");

var eventHubs = builder.AddAzureEventHubs("eventhubs").RunAsEmulator().AddEventHub("myhub");

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithReference(blob)
    .WithReference(queue)
    .WithReference(eventHubs);

var apiService = builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(eventHubs);

builder.AddProject<Projects.AzureFunctionsEndToEnd_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(funcApp)
    .WithReference(apiService);

builder.Build().Run();
