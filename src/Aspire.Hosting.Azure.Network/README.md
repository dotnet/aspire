# Aspire.Hosting.Azure.Network library

Provides extension methods and resource definitions for an Aspire AppHost to configure Azure Virtual Networks, Subnets, and Private Endpoints.

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

### Adding Private Endpoints

Create a private endpoint to securely connect to Azure resources over a private network:

```csharp
var vnet = builder.AddAzureVirtualNetwork("vnet");
var peSubnet = vnet.AddSubnet("private-endpoints", "10.0.2.0/24");

var storage = builder.AddAzureStorage("storage");
var blobs = storage.AddBlobs("blobs");

// Add a private endpoint for the blob storage
builder.AddPrivateEndpoint(peSubnet, blobs);
```

When you add a private endpoint to an Azure resource:

1. A Private DNS Zone is automatically created for the service (e.g., `privatelink.blob.core.windows.net`)
2. A Virtual Network Link connects the DNS zone to your VNet
3. A DNS Zone Group is created on the private endpoint for automatic DNS registration
4. The target resource is automatically configured to deny public network access

To override the automatic network lockdown, use `ConfigureInfrastructure`:

```csharp
storage.ConfigureInfrastructure(infra =>
{
    var storageAccount = infra.GetProvisionableResources()
        .OfType<StorageAccount>()
        .Single();
    storageAccount.PublicNetworkAccess = StoragePublicNetworkAccess.Enabled;
});
```

## Additional documentation

* https://learn.microsoft.com/azure/virtual-network/
* https://learn.microsoft.com/azure/private-link/

## Feedback & contributing

https://github.com/dotnet/aspire
