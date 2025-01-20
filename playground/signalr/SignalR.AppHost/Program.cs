using Aspire.Hosting.Azure;
using Azure.Provisioning;
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
    .ConfigureInfrastructure(infra =>
    {
        var resource = infra.GetProvisionableResources().OfType<SignalRService>().First(s => s.BicepIdentifier == infra.AspireResource.GetBicepIdentifier());
        resource.Features.Add(new SignalRFeature()
        {
            Flag = SignalRFeatureFlag.ServiceMode,
            Value = "Serverless"
        });
        // Add required role assignment to invoke REST API for serverless mode
        var principalTypeParameter = infra.GetProvisionableResources().OfType<ProvisioningParameter>().First(o => o.BicepIdentifier == AzureBicepResource.KnownParameters.PrincipalType);
        var principalIdTypeParameter = infra.GetProvisionableResources().OfType<ProvisioningParameter>().First(o => o.BicepIdentifier == AzureBicepResource.KnownParameters.PrincipalId);
        infra.Add(resource.CreateRoleAssignment(SignalRBuiltInRole.SignalRRestApiOwner, principalTypeParameter, principalIdTypeParameter));
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
