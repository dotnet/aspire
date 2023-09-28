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

Call `AddAzureBlobService` extension method to add the `BlobServiceClient` with the desired configurations exposed with `AzureStorageBlobsSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureStorageBlobsSettings` from configuration by using `Aspire:Azure:Storage:Blobs` key. Example `appsettings.json` that configures some of the settings, note that `ServiceUri` is required to be set:

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Blobs": {
          "ServiceUri": "YOUR_SERVICEURI",
          "HealthChecks": true,
          "Tracing": false,
          "ClientOptions": {
            "EnableTenantDiscovery": true
          }
        }
      }
    }
  }
}
```

If you have setup your configurations in the `Aspire.Azure.Storage.Blobs` section you can just call the method without passing any parameter.

```cs
    builder.AddAzureBlobService();
```

If you want to add more than one [BlobServiceClient](https://learn.microsoft.com/dotnet/api/azure.storage.blobs.blobserviceclient) you could use named instances. The json configuration would look like: 

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Blobs": {
          "INSTANCE_NAME": {
            "ServiceUri": "YOUR_URI",
            "HealthChecks": false,
            "ClientOptions": {
              "EnableTenantDiscovery": true
            }
          }
        }
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddAzureBlobService` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddAzureBlobService("INSTANCE_NAME");
```

Also you can pass the `Action<AzureStorageBlobsSettings>` delegate to set up some or all the options inline, for example to set the `ServiceUri`:

```cs
    builder.AddAzureBlobService(settings => settings.ServiceUri = new Uri("YOUR_SERVICEURI"));
```

Here are the configurable options with corresponding default values:

```cs
public sealed class AzureStorageBlobsSettings
{
    //  A "Uri" referencing the blob service.
    public Uri? ServiceUri { get; set; }

    // The credential used to authenticate to the Blob Storage.
    public TokenCredential? Credential { get; set; }

    // A boolean value that indicates whether the Blob Storage health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = false
}
```

You can also setup the [BlobClientOptions](https://learn.microsoft.com/dotnet/api/azure.storage.blobs.blobclientoptions) using `Action<IAzureClientBuilder<BlobServiceClient, BlobClientOptions>>` delegate, the second parameter of the `AddAzureBlobService` method. For example to set the `EnableTenantDiscovery`:

```cs
    builder.AddAzureBlobService(null, clientBuilder => clientBuilder.ConfigureOptions(options => options.EnableTenantDiscovery = true));
```

After adding a `BlobServiceClient` to the builder you can get the `BlobServiceClient` instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
