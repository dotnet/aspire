# Aspire.Hosting.Azure.SignalR library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure SignalR.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure SignalR Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.SignalR
```

## Configure Azure Provisioning for local development

Adding Azure resources to the .NET Aspire application model will automatically enable development-time provisioning
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

## Usage example

In the _AppHost.cs_ file of `AppHost`, add a SignalR connection and consume the connection using the following methods:

```csharp
var signalR = builder.AddAzureSignalR("sr");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(signalR);
```

The `WithReference` method configures a connection in the `MyService` project named `sr`. In the _Program.cs_ file of `MyService`, the Azure SignalR connection can be consumed using the client library [Microsoft.Azure.SignalR](https://www.nuget.org/packages/Microsoft.Azure.SignalR):

```csharp
builder.Services.AddSignalR()
    .AddNamedAzureSignalR("sr");
```

## Additional documentation

* https://github.com/dotnet/aspire/tree/main/src/Components/README.md
* https://learn.microsoft.com/dotnet/aspire/real-time/azure-signalr-scenario
* https://learn.microsoft.com/azure/azure-signalr/signalr-overview

## Feedback & contributing

https://github.com/dotnet/aspire
