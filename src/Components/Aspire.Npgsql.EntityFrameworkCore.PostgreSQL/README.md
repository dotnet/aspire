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

Call `AddNpgsqlDbContext` extension method to add the `DbContext`  with the desired configurations exposed with `NpgsqlEntityFrameworkCorePostgreSQLSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `NpgsqlEntityFrameworkCorePostgreSQLSettings` from configuration by using `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL` key. Example `appsettings.json` that configures some of the options, note that `ConnectionString` is  required to be set:

```json
{
  "Aspire": {
    "Npgsql": {
      "EntityFrameworkCore": {
        "PostgreSQL": {
          "ConnectionString": "YOUR_CONNECTIONSTRING",
          "DbContextPooling": true,
          "HealthChecks": false,
          "Tracing": false
        }
      }
    }
  }
}
```

If you have setup your configurations in the `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL` section you can just call the method without passing any parameter.
 
```cs
    builder.AddNpgsqlDbContext<YourDbContext>();
```

If you want to register more than one `DbContext` with different configuration, you can use `$"Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:{typeof(TContext).Name}"` configuration section name. The json configuration would look like:

```json
{
  "Aspire": {
    "Npgsql": {
      "EntityFrameworkCore": {
        "PostgreSQL": {
          "ConnectionString": "DEFAULT_CONNECTIONSTRING",
          "DbContextPooling": true,
          "Tracing": false,
          "AnotherDbContext": {
            "ConnectionString": "AnotherDbContext_CONNECTIONSTRING",
            "Tracing": true
          }
        }
      }
    }
  }
}
```

Then calling the `AddNpgsqlDbContext` method with `AnotherDbContext` type parameter would load the settings from `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:AnotherDbContext` section. 

```cs
    builder.AddNpgsqlDbContext<AnotherDbContext>();
```

Also you can pass the `Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>` delegate to set up some or all the options inline, for example to set the `ConnectionString`:

```cs
    builder.AddNpgsqlDbContext<YourDbContext>(settings => settings.ConnectionString = "YOUR_CONNECTIONSTRING");
```

Here are the configurable options with corresponding default values:

```cs
public sealed class NpgsqlEntityFrameworkCorePostgreSQLSettings
{
    // The connection string of the SQL Server database to connect to.
    public string? ConnectionString { get; set; }

    // A boolean value that indicates whether the DB context will be pooled or explicitly created every time it's requested.
    public bool DbContextPooling { get; set; } = true;
	
    // The maximum number of retry attempts. Default value is 6, set it to 0 to disable the retry mechanism.
    public int MaxRetryCount { get; set; } = 6;

    // A boolean value that indicates whether the database health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = true;
	
    // A boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    public bool Metrics { get; set; } = true;
}
```

After adding a `YourDbContext` to the builder you can get the `YourDbContext` instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
