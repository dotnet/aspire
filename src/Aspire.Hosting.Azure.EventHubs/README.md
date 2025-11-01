# Aspire.Hosting.Azure.EventHubs library

Provides extension methods and resource definitions for an Aspire AppHost to configure Azure Event Hubs.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the Aspire Azure Event Hubs Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.EventHubs
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

## Connection Properties

When you reference Azure Event Hubs resources using `WithReference`, the following connection properties are made available to the consuming project:

### Event Hubs namespace

The Event Hubs namespace resource exposes the following connection properties:

| Property Name | Description |
|---------------|-------------|
| `Host`        | The hostname of the Event Hubs namespace |
| `Uri`         | The connection URI for the Event Hubs namespace, with the format `sb://myeventhubs.servicebus.windows.net` |
| `Azure`       | Indicates this is an Azure resource (`true` for Azure, `false` when using the emulator) |

### Event Hub

The Event Hub resource inherits all properties from its parent Event Hubs namespace and adds:

| Property Name | Description |
|---------------|-------------|
| `EventHubName` | The name of the event hub |

### Event Hub consumer group

The Event Hub consumer group resource inherits all properties from its parent Event Hub and adds:

| Property Name | Description |
|---------------|-------------|
| `ConsumerGroup` | The name of the consumer group |

These properties are automatically injected into your application's environment variables or available to create custom values.

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Microsoft.Azure.EventHubs/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
