# Aspire.Microsoft.Azure.Cosmos library

Registers [CosmosClient](https://learn.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient) as a singleton in the DI container for connecting to Azure Cosmos DB. Enables corresponding logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Cosmos DB account - [create a Cosmos DB account](https://learn.microsoft.com/azure/cosmos-db/nosql/how-to-create-account)

### Install the package

Install the .NET Aspire Microsft Azure Cosmos DB library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Microsoft.Azure.Cosmos
```

## Usage example

In the _Program.cs_ file of your project, call the `AddAzureCosmosDB` extension method to register a `CosmosClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureCosmosDB("cosmosConnectionName");
```

You can then retrieve the `CosmosClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly CosmosClient _client;

public ProductsController(CosmosClient client)
{
    _client = client;
}
```

See the [Azure Cosmos DB documentation](https://learn.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient) for examples on using the `CosmosClient`.

## Configuration

The .NET Aspire Azure Cosmos DB library provides multiple options to configure the Azure Cosmos DB connection based on the requirements and conventions of your project. Note that either an `AccountEndpoint` or a `ConnectionString` is a required to be supplied.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureCosmosDB()`:

```csharp
builder.AddAzureCosmosDB("cosmosConnectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Account Endpoint

The recommended approach is to use an AccountEndpoint, which works with the `AzureCosmosDBSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "cosmosConnectionName": "https://{account_name}.documents.azure.com:443/"
  }
}
```

#### Connection string

Alternatively, an [Azure Cosmos DB connection string](https://learn.microsoft.com/azure/cosmos-db/nosql/how-to-dotnet-get-started#connect-with-a-connection-string) can be used.

```json
{
  "ConnectionStrings": {
    "cosmosConnectionName": "AccountEndpoint=https://{account_name}.documents.azure.com:443/;AccountKey={account_key};"
  }
}
```

### Use configuration providers

The .NET Aspire Microsoft Azure Cosmos DB library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureCosmosDBSettings` and `QueueClientOptions` from configuration by using the `Aspire:Microsoft:Azure:Cosmos` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Microsoft": {
      "Azure": {
        "Cosmos": {
          "Tracing": true,
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<AzureCosmosDBSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
    builder.AddAzureCosmosDB("cosmosConnectionName", settings => settings.Tracing = false);
```

You can also setup the [CosmosClientOptions](https://learn.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclientoptions) using the optional `Action<CosmosClientOptions> configureClientOptions` parameter of the `AddAzureCosmosDB` method. For example, to set the `ApplicationName` "User-Agent" header suffix for all requests issues by this client:

```csharp
    builder.AddAzureCosmosDB("cosmosConnectionName", configureClientOptions: clientOptions => clientOptions.ApplicationName = "myapp");
```

## AppHost extensions

In your AppHost project, add a Cosmos DB connection and consume the connection using the following methods:

```csharp
var cosmosdb = builder.AddAzureCosmosDB("cdb").AddDatabase("cosmosdb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(cosmosdb);
```

The `AddAzureCosmosDB` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:cosmosdb` config key. The `WithReference` method passes that connection information into a connection string named `cosmosdb` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddAzureCosmosDB("cosmosdb");
```

## Additional documentation

* https://learn.microsoft.com/azure/cosmos-db/nosql/sdk-dotnet-v3
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
