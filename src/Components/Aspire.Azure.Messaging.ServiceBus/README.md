# Aspire.Azure.Messaging.ServiceBus

Registers a [ServiceBusClient](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusclient) in the DI container for connecting to Azure Service Bus.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Service Bus namespace, learn more about how to [add a Service Bus namespace](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?#create-a-namespace-in-the-azure-portal). Alternatively, you can use a connection string, which is not recommended in production environments.

### Install the package

Install the Aspire Azure Service Bus library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Azure.Messaging.ServiceBus
```

## Usage Example

In the `Program.cs` file of your project, call the `AddAzureServiceBus` extension to register a `ServiceBusClient` for use via the dependency injection container. The method takes a connection name parameter.

```cs
builder.AddAzureServiceBus("sb");
```

You can then retrieve the `ServiceBusClient` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly ServiceBusClient _client;

public ProductsController(ServiceBusClient client)
{
    _client = client;
}
```

See the [Azure.Messaging.ServiceBus documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/README.md) for examples on using the `ServiceBusClient`.

## Configuration

The Aspire Azure Service Bus library provides multiple options to configure the Azure Service Bus connection based on the requirements and conventions of your project. Note that either a `Namespace` or a `ConnectionString` is a required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureServiceBus()`:

```cs
builder.AddAzureServiceBus("serviceBusConnectionName");
```

And then the connection information will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Fully Qualified Namespace

The recommended approach is to use a fully qualified namespace, which works with the `AzureMessagingServiceBusSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "serviceBusConnectionName": "{your_namespace}.servicebus.windows.net"
  }
}
```

#### Connection String

Alternatively, a connection string can be used.

```json
{
  "ConnectionStrings": {
    "serviceBusConnectionName": "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=accesskeyname;SharedAccessKey=accesskey"
  }
}
```

### Use configuration providers

The Aspire Azure Service Bus library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureMessagingServiceBusSettings` and `ServiceBusClientOptions` from configuration by using the `Aspire:Azure:Messaging:ServiceBus` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Messaging": {
        "ServiceBus": {
          "HealthChecks": false,
          "Tracing": true,
          "ClientOptions": {
            "Identifier": "CLIENT_ID"
          }
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<AzureMessagingServiceBusSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```cs
    builder.AddAzureServiceBus("sb", settings => settings.HealthChecks = false);
```

You can also setup the [ServiceBusClientOptions](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusclientoptions) using the optional `Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>> configureClientBuilder` parameter of the `AddAzureServiceBus` method. For example, to set the client ID for this client:

```cs
    builder.AddAzureServiceBus("sb", configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Identifier = "CLIENT_ID"));
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/aspire

