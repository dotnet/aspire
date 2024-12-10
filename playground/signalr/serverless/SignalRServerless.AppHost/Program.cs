// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.SignalR;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");

var signalr = builder
    .AddAzureSignalR("signalr1")
    .ConfigureInfrastructure(i =>
    {
        var resource = i.GetProvisionableResources().OfType<SignalRService>().First(s => s.BicepIdentifier == i.AspireResource.GetBicepIdentifier());
        resource.Features.Add(new SignalRFeature()
        {
            Flag = SignalRFeatureFlag.ServiceMode,
            Value = "Serverless"
        });
    })
    .RunAsEmulator();

builder.AddAzureFunctionsProject<Projects.SignalRServerless_Functions>("funcapp")
    .WithHostStorage(storage)
    .WithExternalHttpEndpoints()
    .WithEnvironment("AzureSignalRConnectionString", signalr) // Injected connection string as env variable
    .WithReference(signalr)
    .WithReference(blob)
    .WithReference(queue);

builder.Build().Run();
