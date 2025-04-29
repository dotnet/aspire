# Aspire.Hosting.Azure.EventHubs library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Event Hubs.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure Event Hubs Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.EventHubs
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

In the _AppHost.cs_ file of `AppHost`, add an Event Hubs connection and consume the connection using the following methods:

```csharp
var eventHubs = builder.AddAzureEventHubs("eh");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(eventHubs);
```

The `WithReference` method passes that connection information into a connection string named `eh` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.Messaging.EventHubs](https://www.nuget.org/packages/Aspire.Azure.Messaging.EventHubs):

```csharp
builder.AddAzureEventProcessorClient("eh");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Microsoft.Azure.EventHubs/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
