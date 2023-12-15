# Aspire.Oracle.EntityFrameworkCore.Database library

Registers [EntityFrameworkCore](https://learn.microsoft.com/ef/core/) [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext) service for connecting Oracle database. Enables connection pooling, health check, logging and telemetry.

## Getting started

### Prerequisites

- Oracle database and connection string for accessing the database.

### Install the package

Install the .NET Aspire Oracle EntityFrameworkCore Database library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Oracle.EntityFrameworkCore.Database
```

## Usage example

In the _Program.cs_ file of your project, call the `AddOracleDatabaseDbContext` extension method to register a `DbContext` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddOracleDatabaseDbContext<MyDbContext>("orcl");
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

The .NET Aspire Oracle EntityFrameworkCore Database component provides multiple options to configure the SQL connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddOracleDatabaseDbContext<TContext>()`:

```csharp
builder.AddOracleDatabaseDbContext<MyDbContext>("myConnection");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myConnection": "Data Source=TORCL;User Id=myUsername;Password=myPassword;"
  }
}
```

See the [ODP.NET documentation](https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/#Oracle%C2%AE-Data-Provider-for-.NET) for more information on how to format this connection string.

### Use configuration providers

The .NET Aspire Oracle EntityFrameworkCore Database component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `OracleEntityFrameworkCoreDatabaseSettings` from configuration by using the `Aspire:Microsoft:EntityFrameworkCore:OracleDatabase` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Oracle": {
      "EntityFrameworkCore": {
        "Database": {
          "DbContextPooling": true,
          "HealthChecks": false,
          "Tracing": false,
          "Metrics": true
        }
      }
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<OracleEntityFrameworkCoreDatabaseSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
    builder.AddOracleDatabaseDbContext<MyDbContext>("orcl", settings => settings.HealthChecks = false);
```

## AppHost extensions 
  
 In your AppHost project, register an Oracle container and consume the connection using the following methods: 
  
 ```csharp 
 var oracledb = builder.AddPostgresContainer("oracle").AddDatabase("freepdb1"); 
  
 var myService = builder.AddProject<Projects.MyService>() 
                        .WithReference(oracledb); 
 ``` 

## Additional documentation

* https://learn.microsoft.com/ef/core/
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
