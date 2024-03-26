# Aspire.Hosting.Azure.ServiceBus library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Service Bus.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Service Bus namespace, learn more about how to [add a Service Bus namespace](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?#create-a-namespace-in-the-azure-portal). Alternatively, you can use a connection string, which is not recommended in production environments.

### Install the package

Install the .NET Aspire Azure Service Bus Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.ServiceBus
```

## Usage example

In the _Program.cs_ file of `AppHost`, add a Service Bus connection and consume the connection using the following methods:

```csharp
var serviceBus = builder.AddAzureServiceBus("sb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(serviceBus);
```

The `AddAzureServiceBus` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:sb` config key. The `WithReference` method passes that connection information into a connection string named `sb` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using the client library [Aspire.Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Aspire.Azure.Messaging.ServiceBus):

```csharp
builder.AddAzureServiceBusClient("sb");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
