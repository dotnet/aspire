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

In the `Program.cs` file of your project, call the `AddAzureQueueService` extension to register a `QueueServiceClient` for use via the dependency injection container.

```cs
builder.AddAzureQueueService();
```

You can then retrieve the `QueueServiceClient` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly QueueServiceClient _client;

public ProductsController(QueueServiceClient client)
{
    _client = client;
}
```

See the [Azure.Storage.Queues documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Queues/README.md) for examples on using the `QueueServiceClient`.

## Configuration

The Aspire Azure Storage Queues library provides multiple options to configure the Azure Storage Queues connection based on the requirements and conventions of your project. Note that either a `ServiceUri` or a `ConnectionString` is a required to be supplied.

### Use configuration providers

The Aspire Azure Storage Queues library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureStorageQueuesSettings` and `QueueClientOptions` from configuration by using the `Aspire:Azure:Storage:Queues` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Queues": {
          "ServiceUri": "https://{account_name}.queue.core.windows.net/",
          "HealthChecks": false,
          "Tracing": true,
          "ClientOptions": {
            "Diagnostics": {
              "ApplicationId": "myapp"
            }
          }
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<AzureStorageQueuesSettings> configureSettings` delegate to set up some or all the options inline, for example to set the `ServiceUri`:

```cs
    builder.AddAzureQueueService(configureSettings: settings => settings.ServiceUri = new Uri("https://{account_name}.queue.core.windows.net/"));
```

You can also setup the [QueueClientOptions](https://learn.microsoft.com/dotnet/api/azure.storage.queues.queueclientoptions) using the `Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>> configureClientBuilder` delegate, the second parameter of the `AddAzureQueueService` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```cs
    builder.AddAzureQueueService(configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureQueueService()`:

```cs
builder.AddAzureQueueService("queueConnectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "queueConnectionName": "AccountName=myaccount;AccountKey=myaccountkey"
  }
}
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Queues/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
