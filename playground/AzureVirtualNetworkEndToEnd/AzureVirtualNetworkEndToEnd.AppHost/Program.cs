// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Network;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

// Create a virtual network with two subnets:
// - One for the Container App Environment (with service delegation)
// - One for private endpoints
var vnet = builder.AddAzureVirtualNetwork("vnet");

var containerAppsSubnet = vnet.AddSubnet("container-apps", subnetName: null, "10.0.0.0/23")
    .WithAnnotation(
        new AzureSubnetServiceDelegationAnnotation("ContainerAppsDelegation", "Microsoft.App/environments"));

var privateEndpointsSubnet = vnet.AddSubnet("private-endpoints", subnetName: null, "10.0.2.0/27");

// Configure the Container App Environment to use the VNet
builder.AddAzureContainerAppEnvironment("env")
    .ConfigureInfrastructure(infra =>
    {
        var env = infra.GetProvisionableResources()
            .OfType<ContainerAppManagedEnvironment>()
            .Single();

        env.VnetConfiguration = new ContainerAppVnetConfiguration
        {
            InfrastructureSubnetId = containerAppsSubnet.Resource.Id.AsProvisioningParameter(infra)
        };
    });

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var mycontainer = storage.AddBlobContainer("mycontainer");

var queues = storage.AddQueues("queues");
var myqueue = storage.AddQueue("myqueue");

// Add private endpoints for blob and queue storage
// This automatically:
// - Creates Private DNS Zones for each service
// - Links the DNS zones to the VNet
// - Creates the Private Endpoints
// - Locks down public access to the storage account
builder.AddAzurePrivateEndpoint(privateEndpointsSubnet, blobs);
builder.AddAzurePrivateEndpoint(privateEndpointsSubnet, queues);

builder.AddProject<Projects.AzureVirtualNetworkEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(mycontainer).WaitFor(mycontainer)
       .WithReference(myqueue).WaitFor(myqueue);

builder.Build().Run();
