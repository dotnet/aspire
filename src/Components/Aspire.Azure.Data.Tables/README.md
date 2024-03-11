# Aspire.Azure.Data.Tables library

Registers [TableServiceClient](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableserviceclient) as a singleton in the DI container for connecting to Azure Table storage. Enables corresponding health check, logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- An Azure storage account or Azure Cosmos DB database with Azure Table API specified. - [create a storage account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

### Install the package

Install the .NET Aspire Azure Table storage library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.Data.Tables
```

## Usage example

In the _Program.cs_ file of your project, call the `AddAzureTableClient` extension method to register a `TableServiceClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureTableClient("tables");
```

You can then retrieve the `TableServiceClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly TableServiceClient _client;

public ProductsController(TableServiceClient client)
{
    _client = client;
}
```

See the [Azure.Data.Tables documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/tables/Azure.Data.Tables/README.md) for examples on using the `TableServiceClient`.

## Configuration

The .NET Aspire Azure Table storage library provides multiple options to configure the Azure Table connection based on the requirements and conventions of your project. Note that either a `ServiceUri` or a `ConnectionString` is a required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureTableClient()`:

```csharp
builder.AddAzureTableClient("tableConnectionName");
```

And then the connection information will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Service URI

The recommended approach is to use a ServiceUri, which works with the `AzureDataTablesSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "tableConnectionName": "https://{account_name}.table.core.windows.net/"
  }
}
```

#### Connection string

Alternatively, an [Azure Storage connection string](https://learn.microsoft.com/azure/storage/common/storage-configure-connection-string) can be used.

```json
{
  "ConnectionStrings": {
    "tableConnectionName": "AccountName=myaccount;AccountKey=myaccountkey"
  }
}
```

### Use configuration providers

The Azure Table storage library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureDataTablesSettings` and `TableClientOptions` from configuration by using the `Aspire:Azure:Data:Tables` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Data": {
        "Tables": {
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

You can also pass the `Action<AzureDataTablesSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
builder.AddAzureTableClient("tables", settings => settings.HealthChecks = false);
```

You can also setup the [TableClientOptions](https://learn.microsoft.com/dotnet/api/azure.data.tables.tableclientoptions) using the optional `Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>> configureClientBuilder` parameter of the `AddAzureTableClient` method. For example, to set the first part of "User-Agent" headers for all requests issues by this client:

```csharp
builder.AddAzureTableClient("tables", configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "myapp"));
```

## AppHost extensions

In your AppHost project, install the Aspire Azure Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure
```

Then, in the _Program.cs_ file of `AppHost`, add a Table Storage connection and consume the connection using the following methods:

```csharp
var tables = builder.AddAzureStorage("storage").AddTables("tables");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(tables);
```

The `AddTables` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:tables` config key. The `WithReference` method passes that connection information into a connection string named `tables` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddAzureTableClient("tables");
```

## Additional documentation

* https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/tables/Azure.Data.Tables/README.md
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
