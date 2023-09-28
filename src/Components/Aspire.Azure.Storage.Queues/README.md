# Aspire.Azure.Storage.Queues library

Registers [QueueServiceClient](https://learn.microsoft.com/dotnet/api/azure.storage.queues.queueserviceclient) as a singleton in the DI container for connecting to Azure Queue Storage. Enables corresponding health check, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Storage account - [create a storage account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

### Install the package

Install the Aspire Azure Storage Queues library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Azure.Storage.Queues
```

## Usage Example

Call `AddAzureQueueService` extension method to add the `QueueServiceClient` with the desired configurations exposed with `AzureStorageQueuesSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureStorageQueuesSettings` from configuration by using `Aspire:Azure:Storage:Queues` key. Example `appsettings.json` that configures some of the settings, note that `ServiceUri` is required to be set:

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Queues": {
          "ServiceUri": "YOUR_URI",
          "HealthChecks": false,
          "Tracing": true,
          "ClientOptions": {
            "MessageEncoding": "Base64"
          }
        }
      }
    }
  }
}
```

If you have setup your configurations in the `Aspire.Azure.Storage.Queues` section you can just call the method without passing any parameter.

```cs
    builder.AddAzureQueueService();
```

If you want to add more than one [QueueServiceClient](https://learn.microsoft.com/dotnet/api/azure.storage.queues.queueserviceclient) you could use named instances. The json configuration would look like: 

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Queues": {
          "INSTANCE_NAME": {
            "ServiceUri": "YOUR_URI",
            "HealthChecks": false,
            "ClientOptions": {
              "MessageEncoding": "Base64"
            }
          }
        }
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddAzureQueueService` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddAzureQueueService("INSTANCE_NAME");
```

Also you can pass the `Action<AzureStorageQueuesSettings>` delegate to set up some or all the options inline, for example to set the `ServiceUri`:

```cs
    builder.AddAzureQueueService(settings => settings.ServiceUri = new Uri("YOUR_SERVICE_URI"));
```

Here are the configurable settings with corresponding default values:

```cs
public sealed class AzureStorageQueuesSettings
{
    // A "Uri" referencing the queue service.
    public Uri? ServiceUri { get; set; }

    // The credential used to authenticate to the Queues Storage.
    public TokenCredential? Credential { get; set; }

    // A boolean value that indicates whether the health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = false;
}
```

You can also setup the [QueueClientOptions](https://learn.microsoft.com/dotnet/api/azure.storage.queues.queueclientoptions) using `Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>` delegate, the second parameter of the `AddAzureQueueService` method. For example to set the `MessageEncoding`:

```cs
    builder.AddAzureQueueService(null, clientBuilder => clientBuilder.ConfigureOptions(options => options.MessageEncoding = QueueMessageEncoding.Base64));
```

After adding a `QueueServiceClient` to the builder you can get the `QueueServiceClient` instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
