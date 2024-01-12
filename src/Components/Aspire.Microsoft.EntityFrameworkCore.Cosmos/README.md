# Aspire.Microsoft.EntityFrameworkCore.Cosmos library

Registers [EntityFrameworkCore](https://learn.microsoft.com/en-us/ef/core/) [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext) in the DI container for connecting to Azure Cosmos DB. Enables connection pooling, logging and telemetry.

## Getting started

### Prerequisites

- CosmosDB database and connection string for accessing the database.

### Install the package

Install the .NET Aspire Microsoft EntityFrameworkCore Cosmos library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Microsoft.EntityFrameworkCore.Cosmos
```

## Usage example

In the _Program.cs_ file of your project, call the `AddCosmosDbContext` extension method to register a `DbContext` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddCosmosDbContext<MyDbContext>("cosmosdb");
```

You can then retrieve the `MyDbContext` instance using dependency injection. For example, to retrieve the context from a Web API controller:

```csharp
private readonly MyDbContext _context;

public ProductsController(MyDbContext context)
{
    _context = context;
}
```

## Configuration

The .NET Aspire Microsoft EntityFrameworkCore Cosmos component provides multiple options to configure the database connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddCosmosDbContext()`:

```csharp
builder.AddCosmosDbContext<MyDbContext>("myConnection");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myConnection": "AccountEndpoint=https://{account_name}.documents.azure.com:443/;AccountKey={account_key};"
  }
}
```

See the [ConnectionString documentation](https://learn.microsoft.com/azure/cosmos-db/nosql/how-to-dotnet-get-started#connect-with-a-connection-string) for more information.

### Use configuration providers

The .NET Aspire Microsoft EntityFrameworkCore Cosmos component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `EntityFrameworkCoreCosmosDBSettings` from configuration by using the `Aspire:Microsaoft:EntityFrameworkCore:Cosmos` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Microsoft": {
      "EntityFrameworkCore": {
        "Cosmos": {
          "DbContextPooling": true,
          "Tracing": false
        }
      }
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<EntityFrameworkCoreCosmosDBSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
    builder.AddCosmosDbContext<MyDbContext>("cosmosdb", settings => settings.Tracing = false);
```

## AppHost extensions

In your AppHost project, install the Aspire Azure Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure
```

Then, in the _Program.cs_ file of `AppHost`, add a Cosmos DB connection and consume the connection using the following methods::

```csharp
var cosmosdb = builder.AddAzureCosmosDB("cdb").AddDatabase("cosmosdb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(cosmosdb);
```

The `AddAzureCosmosDB` method will read connection information from the AppHost's configuration (for example, from "user secrets") under the `ConnectionStrings:cosmosdb` config key. The `WithReference` method passes that connection information into a connection string named `cosmosdb` in the `MyService` project. In the _Program.cs_ file of `MyService`, the connection can be consumed using:

```csharp
builder.AddCosmosDbContext<MyDbContext>("cosmosdb");
```

## Additional documentation

* https://learn.microsoft.com/ef/core/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
