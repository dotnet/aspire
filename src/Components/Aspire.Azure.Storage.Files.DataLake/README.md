# Aspire.Azure.Storage.Files.DataLake

Registers a [DataLakeServiceClient](https://learn.microsoft.com/en-us/dotnet/api/azure.storage.files.datalake.datalakeserviceclient?view=azure-dotnet) service or  as a singleton in the DI container for connecting to Azure Data Lake Storage. Enables corresponding health checks, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Storage account - [create a storage account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

### Install the package

Install the .NET Aspire Azure Storage DataLake library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.Storage.Files.DataLake
```

## Configuration

The .NET Aspire Azure DataLake Storage library provides multiple options to configure the Azure Data Lake Storage connection based on the requirements and conventions of your project. Note that either a `ServiceUri` or a `ConnectionString` is a required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureDataLakeServiceClient()`:

```csharp
builder.AddAzureDataLakeClientServiceClient("dataLakeConnectionName");
```

And then the connection information will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Service URI

The recommended approach is to use a ServiceUri, which works with the `AzureDataLakeSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "dataLakeConnectionName": "https://{account_name}.dfs.core.windows.net/"
  }
}
```

#### Connection string

Alternatively, an [Azure Storage connection string](https://learn.microsoft.com/azure/storage/common/storage-configure-connection-string) can be used.

```json
{
  "ConnectionStrings": {
    "dataLakeConnectionName": "AccountName=myaccount;AccountKey=myaccountkey"
  }
}
```

### Use configuration providers

The .NET Aspire Azure Data Lake Storage library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureDataLakeSettings` and `DataLakeClientOptions` from configuration by using the `Aspire:Azure:Storage:Files:DataLake` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Storage": {
        "Files": {
          "DataLake": {
            "ServiceUri": "http://YOUR_URI",
            "DisableHealthChecks": true,
            "DisableTracing": false,
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
}
```

### Use inline delegates

You can also pass the `Action<AzureDataLakeSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
builder.AddAzureDataLakeServiceClient("data-lake", settings => settings.HealthChecks = false);
```

You can also setup the [DataLakeClientOptions](https://learn.microsoft.com/dotnet/api/azure.storage.files.datalake.datalakeclientoptions?view=azure-dotnet) using the optional `Action<IAzureClientBuilder<DataLakeServiceClient, DataLakeClientOptions>> configureClientBuilder` parameter of the `AddAzureDataLakeServiceClient` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```csharp
builder.AddAzureDataLakeServiceClient("dataLakeConnectionName", configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Files.DataLake/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
