# Aspire.Elastic.Clients.Elasticsearch

Registers a [ElasticsearchClient](https://github.com/elastic/elasticsearch-net) in the DI container for connecting to a Elasticsearch.

## Getting started

### Prerequisites

- Elasticsearch cluster.
- Endpoint URI string for accessing the Elasticsearch API endpoint or a CloudId and an ApiKey from [Elastic Cloud](https://www.elastic.co/cloud)

### Install the package

Install the .NET Aspire Elasticsearch Client library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Elastic.Clients.Elasticsearch
```

## Usage example

In the _AppHost.cs_ file of your project, call the `AddElasticsearchClient` extension method to register a `ElasticsearchClient` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddElasticsearchClient("elasticsearch");
```

## Configuration

The .NET Aspire Elasticsearch Client component provides multiple options to configure the server connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddElasticsearchClient()`:

```csharp
builder.AddElasticsearchClient("elasticsearch");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "elasticsearch": "http://elastic:password@localhost:27011"
  }
}
```

### Use configuration providers

The .NET Aspire Elasticsearch Client component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `ElasticClientsElasticsearchSettings` from configuration by using the `Aspire:Elastic:Clients:Elasticsearch` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Elastic": {
      "Clients": {
        "Elasticsearch": {
            "Endpoint": "http://elastic:password@localhost:27011"
        }
      }
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<ElasticClientsElasticsearchSettings> configureSettings` delegate to set up some or all the options inline, for example to set the API key from code:

```csharp
builder.AddElasticsearchClient("elasticsearch", settings => settings.Endpoint = new Uri("http://elastic:password@localhost:27011"));
```

## AppHost extensions

In your AppHost project, install the `Aspire.Hosting.Elasticsearch` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Elasticsearch
```

Then, in the _AppHost.cs_ file of `AppHost`, register a Elasticsearch cluster and consume the connection using the following methods:

```csharp
var elasticsearch = builder.AddElasticsearch("elasticsearch");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(elasticsearch);
```

The `WithReference` method configures a connection in the `MyService` project named `elasticsearch`. In the _Program.cs_ file of `MyService`, the Elasticsearch connection can be consumed using:

```csharp
builder.AddElasticsearchClient("elasticsearch");
```

### Use a ```CloudId``` and an ```ApiKey``` with configuration providers

When using [Elastic Cloud](https://www.elastic.co/cloud) ,
you can provide the ```CloudId``` and ```ApiKey``` in ```Aspire:Elastic:Clients:Elasticsearch``` section
when calling `builder.AddElasticsearchClient()`.
Example appsettings.json that configures the options:

```csharp
builder.AddElasticsearchClient("elasticsearch");
```

```json
{
  "Aspire": {
    "Elastic": {
      "Clients": {
        "Elasticsearch": {
            "ApiKey": "Valid ApiKey",
            "CloudId": "Valid CloudId"
        }
      }
    }
  }
}
```

### Use a ```CloudId``` and an ```ApiKey``` with inline delegates

```csharp
builder.AddElasticsearchClient("elasticsearch",
settings => {
    settings.CloudId = "Valid CloudId";
    settings.ApiKey = "Valid ApiKey";
});
```

## Additional documentation

* https://github.com/elastic/elasticsearch-net
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
