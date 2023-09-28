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

## Usage example

Call the `AddAzureServiceBus` extension method to add the `ServiceBusClient` with the desired configurations exposed with `AzureMessagingServiceBusSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureMessagingServiceBusSettings` from configuration by using `Aspire:Azure:Messaging:ServiceBus` key. Example `appsettings.json` that configures the `Namespace`, note that the `Namespace` or the `ConnectionString` is required to be set:

```json
{
  "Aspire": {
    "Azure": {
      "Messaging": {
        "ServiceBus": {
          "Namespace": "YOUR_SERVICE_BUS_NAMESPACE",
          "ClientOptions": {
            "Identifier": "CLIENT_ID"
          }
        }
      }
    }
  }
}
```

If you have setup your configurations in the `Aspire.Azure.Messaging.ServiceBus` section you can just call the method without passing any parameter.

```cs
    builder.AddAzureServiceBus();
```

If you want to add more than one [ServiceBusClient](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusclient) you could use named instances. The json configuration would look like: 

```json
{
  "Aspire": {
    "Azure": {
      "Messaging": {
        "ServiceBus": {
          "INSTANCE_NAME": {
            "Namespace": "YOUR_SERVICE_BUS_NAMESPACE",
            "ClientOptions": {
              "Identifier": "CLIENT_ID"
            }
          }
        }
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddAzureServiceBus` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddAzureServiceBus("INSTANCE_NAME");
```

Also you can pass the `Action<AzureMessagingServiceBusSettings>` delegate to set up some or all the options inline, for example to set the `Namespace`:

```cs
    builder.AddAzureServiceBus(settings => settings.Namespace = "YOUR_SERVICE_BUS_NAMESPACE");
```

Here are the configurable options:

```cs
public sealed class AzureMessagingServiceBusSettings
{
    // The connection string used to connect to the Service Bus namespace. 
    public string? ConnectionString { get; set; }

    // The fully qualified Service Bus namespace. 
    public string? Namespace { get; set; }

    // The credential used to authenticate to the Service Bus namespace.
    public TokenCredential? Credential { get; set; }

    // Name of the queue used by the health check. Mandatory to get queue health check enabled.
    public string? HealthCheckQueueName { get; set; }

    // Name of the topic used by the health check. Mandatory to get topic health check enabled.
    public string? HealthCheckTopicName { get; set; }

    // Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; }
}
```

You can also setup the [ServiceBusClientOptions](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusclientoptions) using `Action<IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions>>` delegate, the second parameter of the `AddAzureServiceBus` method. For example to set the `ServiceBusClient` ID to identify the client:

```cs
    builder.AddAzureServiceBus(null, clientBuilder => clientBuilder.ConfigureOptions(options => options.Identifier = "CLIENT_ID"));
```

After adding a `ServiceBusClient` to the builder you can get the `ServiceBusClient` instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
