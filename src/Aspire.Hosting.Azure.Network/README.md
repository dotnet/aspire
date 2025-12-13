# Aspire.Hosting.Azure.Network library

Provides extension methods and resource definitions for an Aspire AppHost to configure Azure Virtual Networks, Subnets, NAT Gateways, and Public IP Addresses.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the Aspire Azure Virtual Network Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Network
```

## Configure Azure Provisioning for local development

Adding Azure resources to the Aspire application model will automatically enable development-time provisioning
for Azure resources so that you don't need to configure them manually. Provisioning requires a number of settings
to be available via .NET configuration. Set these values in user secrets in order to allow resources to be configured
automatically.

```json
{
    "Azure": {
      "SubscriptionId": "<your subscription id>",
      "ResourceGroupPrefix": "<prefix for the resource group>",
      "Location": "<azure location>"
    }
}
```

> NOTE: Developers must have Owner access to the target subscription so that role assignments
> can be configured for the provisioned resources.

## Usage examples

### Adding a Virtual Network

In the _AppHost.cs_ file of `AppHost`, add a Virtual Network using the following method:

```csharp
var vnet = builder.AddAzureVirtualNetwork("vnet");
```

By default, the virtual network will use the address prefix `10.0.0.0/16`. You can specify a custom address prefix:

```csharp
var vnet = builder.AddAzureVirtualNetwork("vnet", "10.1.0.0/16");
```

### Adding Subnets

You can add subnets to your virtual network:

```csharp
var vnet = builder.AddAzureVirtualNetwork("vnet");
var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24");
```

### Adding NAT Gateway with Public IP

Create a NAT Gateway with a Public IP and associate it with a subnet:

```csharp
var publicIp = builder.AddAzurePublicIP("natip");
var natGateway = builder.AddAzureNatGateway("natgw")
                        .WithPublicIP(publicIp);

var vnet = builder.AddAzureVirtualNetwork("vnet");
var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24")
                 .WithNatGateway(natGateway);
```

### Complete example with outbound connectivity

This example creates a Virtual Network with a subnet that has outbound internet connectivity via a NAT Gateway:

```csharp
// Create a public IP for the NAT Gateway
var publicIp = builder.AddAzurePublicIP("natip");

// Create a NAT Gateway and attach the public IP
var natGateway = builder.AddAzureNatGateway("natgw")
                        .WithPublicIP(publicIp);

// Create a Virtual Network with custom address space
var vnet = builder.AddAzureVirtualNetwork("vnet", "10.0.0.0/16");

// Add a subnet with NAT Gateway for outbound connectivity
var subnet = vnet.AddSubnet("appsubnet", "10.0.1.0/24")
                 .WithNatGateway(natGateway);
```

## Additional documentation

* https://learn.microsoft.com/azure/virtual-network/
* https://learn.microsoft.com/azure/nat-gateway/

## Feedback & contributing

https://github.com/dotnet/aspire
