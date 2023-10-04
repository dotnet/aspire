# Aspire.Azure.Storage.Blobs

Registers a [BlobServiceClient](https://learn.microsoft.com/dotnet/api/azure.storage.blobs.blobserviceclient) service as a singleton in the DI container for connecting to Azure Storage Blobs. Enables corresponding health checks, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Storage account - [create a storage account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

### Install the package

Install the Aspire Azure Storage Blobs library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Azure.Storage.Blobs
```

## Usage Example

In the `Program.cs` file of your project, call the `AddAzureBlobService` extension to register a `BlobServiceClient` for use via the dependency injection container.

```cs
builder.AddAzureBlobService();
```

You can then retrieve the `BlobServiceClient` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly BlobServiceClient _client;

public ProductsController(BlobServiceClient client)
{
    _client = client;
}
```

See the [Azure.Storage.Blobs documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/README.md) for examples on using the `BlobServiceClient`.

## Configuration

The Aspire Azure Storage Blobs library provides multiple options to configure the Azure Storage Blob connection based on the requirements and conventions of your project. Note that either a `ServiceUri` or a `ConnectionString` is a required to be supplied.

### Use configuration providers

The Aspire Azure Storage Blobs library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureStorageBlobsSettings` and `BlobClientOptions` from configuration by using the `Aspire:Azure:Storage:Blobs` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Blobs": {
          "ServiceUri": "https://{account_name}.blob.core.windows.net/",
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

You can also pass the `Action<AzureStorageBlobsSettings> configureSettings` delegate to set up some or all the options inline, for example to set the `ServiceUri`:

```cs
    builder.AddAzureBlobService(configureSettings: settings => settings.ServiceUri = new Uri("https://{account_name}.blob.core.windows.net/"));
```

You can also setup the [BlobClientOptions](https://learn.microsoft.com/dotnet/api/azure.storage.blobs.blobclientoptions) using the `Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>> configureClientBuilder` delegate, the second parameter of the `AddAzureBlobService` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```cs
    builder.AddAzureBlobService(configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureBlobService()`:

```cs
builder.AddAzureBlobService("blobsConnectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "blobsConnectionName": "AccountName=myaccount;AccountKey=myaccountkey"
  }
}
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Blobs/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
