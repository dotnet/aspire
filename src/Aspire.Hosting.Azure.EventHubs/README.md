# Aspire.Hosting.Azure.EventHubs library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Event Hubs.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Event Hubs namespace, learn more about how to [add an Event Hubs namespace](https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-create). Alternatively, you can use a connection string, which is not recommended in production environments.

### Install the package

Install the .NET Aspire Azure Event Hubs Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.EventHubs
```

## Usage example

In the _Program.cs_ file of `AppHost`, add an Event Hubs connection and consume the connection using the following methods:

```csharp
var eventHubs = builder.AddAzureEventHubs("eh");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(eventHubs);
```

The `AddAzureEventHubs` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:eh` config key. The `WithReference` method passes that connection information into a connection string named `eh` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.Messaging.EventHubs](https://www.nuget.org/packages/Aspire.Azure.Messaging.EventHubs):

```csharp
builder.AddAzureEventProcessorClient("eh");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Microsoft.Azure.EventHubs/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
