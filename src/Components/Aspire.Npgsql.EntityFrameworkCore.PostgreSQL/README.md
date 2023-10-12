# Aspire.Npgsql.EntityFrameworkCore.PostgreSQL library

Registers [EntityFrameworkCore](https://learn.microsoft.com/en-us/ef/core/) [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext) in the DI container for connecting PostgreSQL database. Enables connection pooling, health check, logging and telemetry.

## Getting started

### Prerequisites

- PostgreSQL database and connection string for accessing the database.

### Install the package

Install the Aspire PostgreSQL EntityFrameworkCore Npgsql library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
```

## Usage Example

In the `Program.cs` file of your project, call the `AddNpgsqlDbContext` extension method to register a `DbContext` for use via the dependency injection container. The method takes a connection name parameter.

```cs
builder.AddNpgsqlDbContext<MyDbContext>("postgresdb");
```

You can then retrieve the `MyDbContext` instance using dependency injection. For example, to retrieve the cache from a Web API controller:

```cs
private readonly MyDbContext _context;

public ProductsController(MyDbContext context)
{
    _context = context;
}
```

## Configuration

The Aspire PostgreSQL EntityFrameworkCore Npgsql component provides multiple options to configure the database connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddNpgsqlDbContext()`:

```cs
builder.AddNpgsqlDbContext<MyDbContext>("myConnection");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myConnection": "Host=myserver;Database=test"
  }
}
```

See the [ConnectionString documentation](https://www.npgsql.org/doc/connection-string-parameters.html) for more information on how to format this connection string.

### Use configuration providers

The Aspire PostgreSQL EntityFrameworkCore Npgsql component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `NpgsqlEntityFrameworkCorePostgreSQLSettings` from configuration by using the `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Npgsql": {
      "EntityFrameworkCore": {
        "PostgreSQL": {
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

Also you can pass the `Action<NpgsqlEntityFrameworkCorePostgreSQLSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```cs
    builder.AddNpgsqlDbContext<MyDbContext>("postgresdb", settings => settings.HealthChecks = false);
```

## App Extensions

In your App project, register a Postgres container and consume the connection using the following methods:

```cs
var postgresdb = builder.AddPostgresContainer("pg").AddDatabase("postgresdb");

var myService = builder.AddProject<YourApp.Projects.MyService>()
                       .WithReference(postgresdb);
```

`.WithReference` configures a connection in the `MyService` project named `postgresdb`. In the `Program.cs` file of `MyService`, the database connection can be consumed using:

```cs
builder.AddNpgsqlDbContext<MyDbContext>("postgresdb");
```

## Additional documentation

* https://learn.microsoft.com/ef/core/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/aspire
