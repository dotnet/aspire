# Aspire.Azure.Search.Documents library

Registers [SearchIndexClient](https://learn.microsoft.com/dotnet/api/azure.search.documents.indexes.searchindexclient) as a singleton in the DI container for connecting to Azure Search. Enables corresponding logging and telemetry.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- Azure Search Service - [create an Azure Search Service resource](https://learn.microsoft.com/azure/search/search-create-service-portal)

### Install the package

Install the .NET Aspire Azure Search library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Azure.Search.Documents
```

## Usage example

In the _Program.cs_ file of your project, call the `AddAzureSearch` extension method to register an `SearchIndexClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureSearch("searchConnectionName");
```

You can then retrieve the `SearchIndexClient` instance using dependency injection. For example, to retrieve the client from a Web API controller:

```csharp
private readonly SearchIndexClient _indexClient;

public SearchController(SearchIndexClient indexClient)
{
    _indexClient = indexClient;
}
```

You can also retrieve a `SearchClient` which can be used for querying, by calling `GetSearchClient(string indexName)` method on the `SearchIndexClient` instance as follows:

```csharp
private readonly SearchIndexClient _indexClient;

public SearchController(SearchIndexClient indexClient)
{
    _indexClient = indexClient;
}

public async Task<long> GetDocumentCountAsync(string indexName, CancellationToken cancellationToken)
{
    var searchClient = _indexClient.GetSearchClient(indexName);
    var documentCountResponse = await searchClient.GetDocumentCountAsync(cancellationToken);
    return documentCountResponse.Value;
}
```

See the [Azure Search Service Getting Started](https://learn.microsoft.com/dotnet/api/overview/azure/search.documents-readme) for examples on using the `SearchIndexClient`.

## Configuration

The .NET Aspire Azure Azure Search library provides multiple options to configure the Azure Search Service based on the requirements and conventions of your project. Note that either an `Endpoint` or a `ConnectionString` is required to be supplied.

### Use a connection string

A connection can be constructed from the __Keys and Endpoint__ tab with the format `Endpoint={endpoint};Key={key};`. You can provide the name of the connection string when calling `builder.AddAzureSearch()`:

```csharp
builder.AddAzureSearch("searchConnectionName");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

#### Account Endpoint

The recommended approach is to use an Endpoint, which works with the `AzureSearchSettings.Credential` property to establish a connection. If no credential is configured, the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) is used.

```json
{
  "ConnectionStrings": {
    "searchConnectionName": "https://{search_service}.search.windows.net/"
  }
}
```

#### Connection string

Alternatively, a custom connection string can be used.

```json
{
  "ConnectionStrings": {
    "searchConnectionName": "Endpoint=https://{search_service}.search.windows.net/;Key={account_key};"
  }
}
```

### Use configuration providers

The .NET Aspire Azure Search library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `AzureSearchSettings` and `SearchClientOptions` from configuration by using the `Aspire:Azure:Search:Documents` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Search": {
        "Documents": {
          "Tracing": true,
        }
      }
    }
  }
}
```

### Use inline delegates

You can also pass the `Action<AzureSearchSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
    builder.AddAzureSearch("searchConnectionName", settings => settings.Tracing = false);
```

You can also setup the [SearchClientOptions](https://learn.microsoft.com/dotnet/api/azure.search.documents.searchclientoptions) using the optional `Action<IAzureClientBuilder<SearchIndexClient, SearchClientOptions>> configureClientBuilder` parameter of the `AddAzureSearch` method. For example, to set the client ID for this client:

```csharp
    builder.AddAzureSearch("searchConnectionName", configureClientBuilder: builder => builder.ConfigureOptions(options => options.Diagnostics.ApplicationId = "CLIENT_ID"));
```

## AppHost extensions

In your AppHost project, install the Aspire Azure Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure
```

Then, in the _Program.cs_ file of `AppHost`, add an Azure Search service and consume the connection using the following methods:

```csharp
var search = builder.AddAzureSearch("search");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(search);
```

The `AddAzureSearch` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:search` config key. The `WithReference` method passes that connection information into a connection string named `search` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddAzureSearch("search");
```

## Additional documentation

* https://learn.microsoft.com/azure/search/search-howto-dotnet-sdk
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
