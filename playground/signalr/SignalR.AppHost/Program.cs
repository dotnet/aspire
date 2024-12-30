using Azure.Provisioning.SignalR;

var builder = DistributedApplication.CreateBuilder(args);

// Configure Azure SignalR in default mode
var defaultSignalr = builder.AddAzureSignalR("signalrDefault");

builder.AddProject<Projects.SignalRWeb>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(defaultSignalr);

// Configure Azure SignalR in serverless mode
var serverlessSignalr = builder
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
    .WithReference(serverlessSignalr)
    .WaitFor(serverlessSignalr);

builder.Build().Run();
