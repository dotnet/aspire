using Azure.Provisioning.SignalR;

var builder = DistributedApplication.CreateBuilder(args);

// Configure Azure SignalR in default mode
var defaultSignalr = builder.AddAzureSignalR("signalrDefault");

builder.AddProject<Projects.SignalRWeb>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(defaultSignalr);

// Configure Azure SignalR in serverless mode
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");

var signalr = builder
    .AddAzureSignalR("signalrServerless")
    .ConfigureInfrastructure(i =>
    {
        var resource = i.GetProvisionableResources().OfType<SignalRService>().First(s => s.BicepIdentifier == i.AspireResource.GetBicepIdentifier());
        resource.Features.Add(new SignalRFeature()
        {
            Flag = SignalRFeatureFlag.ServiceMode,
            Value = "Serverless"
        });
    })
    .RunAsEmulator()
    .WithEndpoint("emulator", e =>
    {
        e.Port = 64323;
    });

builder.AddProject<Projects.SignalRServerlessWeb>("webserverless")
    .WithExternalHttpEndpoints()
    .WithReference(signalr)
    .WaitFor(signalr);

//builder.AddAzureFunctionsProject<Projects.SignalR_Functions>("funcapp")
//    .WithHostStorage(storage)
//    .WithExternalHttpEndpoints()
//    // Injected connection string as env variable
//    .WithEnvironment("AzureSignalRConnectionString", signalr)
//    .WithReference(signalr)
//    .WithReference(blob)
//    .WithReference(queue);

builder.Build().Run();
