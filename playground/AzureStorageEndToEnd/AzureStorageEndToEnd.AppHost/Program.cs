// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Network;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

var vnet = builder.AddAzureVirtualNetwork("vnet");
var subnet1 = vnet.AddSubnet("subnet1", subnetName: null, "10.0.0.0/23")
    .WithAnnotation(
        new AzureSubnetServiceDelegationAnnotation("ContainerAppsDelegation", "Microsoft.App/environments"));

builder.AddAzureContainerAppEnvironment("env")
    .ConfigureInfrastructure(infra =>
    {
        var env = infra.GetProvisionableResources()
            .OfType<ContainerAppManagedEnvironment>()
            .Single();

        env.VnetConfiguration = new ContainerAppVnetConfiguration
        {
            InfrastructureSubnetId = subnet1.Resource.Id.AsProvisioningParameter(infra)
        };
    });

var storage = builder.AddAzureStorage("storage").RunAsEmulator(container =>
{
    container.WithDataBindMount();
});

var blobs = storage.AddBlobs("blobs");
storage.AddBlobContainer("mycontainer1", blobContainerName: "test-container-1");
storage.AddBlobContainer("mycontainer2", blobContainerName: "test-container-2");

var myqueue = storage.AddQueue("myqueue", queueName: "my-queue");

builder.AddProject<Projects.AzureStorageEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(blobs).WaitFor(blobs)
       .WithReference(myqueue).WaitFor(myqueue);

builder.Build().Run();

