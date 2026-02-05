// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// Create a virtual network with two subnets:
// - One for the Container App Environment (with service delegation)
// - One for private endpoints
var vnet = builder.AddAzureVirtualNetwork("vnet");

var containerAppsSubnet = vnet.AddSubnet("container-apps", "10.0.0.0/23");
var privateEndpointsSubnet = vnet.AddSubnet("private-endpoints", "10.0.2.0/27");

// Configure the Container App Environment to use the VNet
builder.AddAzureContainerAppEnvironment("env")
    .WithDelegatedSubnet(containerAppsSubnet);

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
privateEndpointsSubnet.AddPrivateEndpoint(blobs);
privateEndpointsSubnet.AddPrivateEndpoint(queues);

builder.AddProject<Projects.AzureVirtualNetworkEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(mycontainer).WaitFor(mycontainer)
       .WithReference(myqueue).WaitFor(myqueue);

builder.Build().Run();
