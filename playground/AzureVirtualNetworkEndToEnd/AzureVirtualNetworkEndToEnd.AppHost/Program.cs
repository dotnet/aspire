// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Azure.Provisioning.Network is experimental

using Aspire.Hosting.Azure;
using Azure.Provisioning.Network;

var builder = DistributedApplication.CreateBuilder(args);

// Create a virtual network with two subnets:
// - One for the Container App Environment (with service delegation)
// - One for private endpoints
var vnet = builder.AddAzureVirtualNetwork("vnet");

var containerAppsSubnet = vnet.AddSubnet("container-apps", "10.0.0.0/23");
var privateEndpointsSubnet = vnet.AddSubnet("private-endpoints", "10.0.2.0/27");

// Create a NAT Gateway for deterministic outbound IP on the ACA subnet
var natGateway = builder.AddNatGateway("nat");
containerAppsSubnet.WithNatGateway(natGateway);

// Create Network Security Groups for each subnet
var acaNsg = vnet.AddNetworkSecurityGroup("aca-nsg")
    .WithSecurityRule(new AzureSecurityRule
    {
        Name = "allow-https-from-azure-lb",
        Priority = 100,
        Direction = SecurityRuleDirection.Inbound,
        Access = SecurityRuleAccess.Allow,
        Protocol = SecurityRuleProtocol.Tcp,
        SourceAddressPrefix = "AzureLoadBalancer",
        SourcePortRange = "*",
        DestinationAddressPrefix = "*",
        DestinationPortRange = "443"
    })
    .WithSecurityRule(new AzureSecurityRule
    {
        Name = "deny-vnet-inbound",
        Priority = 110,
        Direction = SecurityRuleDirection.Inbound,
        Access = SecurityRuleAccess.Deny,
        Protocol = SecurityRuleProtocol.Asterisk,
        SourceAddressPrefix = "VirtualNetwork",
        SourcePortRange = "*",
        DestinationAddressPrefix = "*",
        DestinationPortRange = "*"
    })
    .WithSecurityRule(new AzureSecurityRule
    {
        Name = "deny-internet-inbound",
        Priority = 4096,
        Direction = SecurityRuleDirection.Inbound,
        Access = SecurityRuleAccess.Deny,
        Protocol = SecurityRuleProtocol.Asterisk,
        SourceAddressPrefix = "Internet",
        SourcePortRange = "*",
        DestinationAddressPrefix = "*",
        DestinationPortRange = "*"
    });

var peNsg = vnet.AddNetworkSecurityGroup("pe-nsg")
    .WithSecurityRule(new AzureSecurityRule
    {
        Name = "allow-https-from-vnet",
        Priority = 100,
        Direction = SecurityRuleDirection.Inbound,
        Access = SecurityRuleAccess.Allow,
        Protocol = SecurityRuleProtocol.Tcp,
        SourceAddressPrefix = "VirtualNetwork",
        SourcePortRange = "*",
        DestinationAddressPrefix = "*",
        DestinationPortRange = "443"
    })
    .WithSecurityRule(new AzureSecurityRule
    {
        Name = "deny-all-internet-inbound",
        Priority = 4096,
        Direction = SecurityRuleDirection.Inbound,
        Access = SecurityRuleAccess.Deny,
        Protocol = SecurityRuleProtocol.Asterisk,
        SourceAddressPrefix = "Internet",
        SourcePortRange = "*",
        DestinationAddressPrefix = "*",
        DestinationPortRange = "*"
    });

containerAppsSubnet.WithNetworkSecurityGroup(acaNsg);
privateEndpointsSubnet.WithNetworkSecurityGroup(peNsg);

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
