# Aspire.Pomelo.EntityFrameworkCore.MySql library

Registers [EntityFrameworkCore](https://learn.microsoft.com/ef/core/) [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext) in the DI container for connecting MySql database. Enables connection pooling, health check, logging and telemetry.

## Getting started

### Prerequisites

- MySQL database and connection string for accessing the database.

### Install the package

Install the .NET Aspire Pomelo EntityFrameworkCore MySQL library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Pomelo.EntityFrameworkCore.MySql
```

## Usage example

In the _Program.cs_ file of your project, call the `AddMySqlDbContext` extension method to register a `DbContext` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddMySqlDbContext<MyDbContext>("mysqldb");
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

The .NET Aspire Pomelo EntityFrameworkCore MySQL component provides multiple options to configure the database connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddMySqlDbContext()`:

```csharp
builder.AddMySqlDbContext<MyDbContext>("myConnection");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myConnection": "Server=myserver;Database=test"
  }
}
```

See the [ConnectionString documentation](https://mysqlconnector.net/connection-options/) for more information on how to format this connection string.

### Use configuration providers

The .NET Aspire Pomelo EntityFrameworkCore MySQL component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration).
It loads the `PomeloEntityFrameworkCoreMySqlSettings` from configuration by using the `Aspire:Pomelo:EntityFrameworkCore:MySql` key.
Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Pomelo": {
      "EntityFrameworkCore": {
        "MySql": {
          "DbContextPooling": true,
          "HealthChecks": false,
          "Tracing": false
        }
      }
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<PomeloEntityFrameworkCoreMySqlSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
    builder.AddMySqlDbContext<MyDbContext>("mysqldb", settings => settings.HealthChecks = false);
```

## AppHost extensions

In your AppHost project, register a MySQL container and consume the connection using the following methods:

```csharp
var mysqldb = builder.AddMySql("mysql").AddDatabase("mysqldb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(mysqldb);
```

The `WithReference` method configures a connection in the `MyService` project named `mysqldb`.
In the _Program.cs_ file of `MyService`, the database connection can be consumed using:

```csharp
builder.AddMySqlDbContext<MyDbContext>("mysqldb");
```

## Additional documentation

* https://learn.microsoft.com/ef/core/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
