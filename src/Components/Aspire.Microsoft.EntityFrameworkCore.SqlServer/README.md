# Aspire.Microsoft.EntityFrameworkCore.SqlServer library

Registers [EntityFrameworkCore](https://learn.microsoft.com/en-us/ef/core/) [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext) service for connecting Azure SQL, MS SQL server database. Enables connection pooling, health check, logging and telemetry.

## Getting started

### Prerequisites

- Azure SQL or MS SQL server database and connection string for accessing the database.

### Install the package

Install the Aspire SQL Server EntityFrameworkCore SqlClient library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Microsoft.EntityFrameworkCore.SqlServer
```

## Usage Example

Call `AddSqlServerDbContext` extension method to add the `DbContext`  with the desired configurations exposed with `MicrosoftEntityFrameworkCoreSqlServerSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `NpgsqlEntityFrameworkCorePostgreSQLSettings` from configuration by using `Aspire:Microsoft:EntityFrameworkCore:SqlServer` key. Example `appsettings.json` that configures some of the options, note that `ConnectionString` is  required to be set:

```json
{
  "Aspire": {
    "Microsoft": {
      "EntityFrameworkCore": {
        "SqlServer": {
          "ConnectionString": "YOUR_CONNECTIONSTRING",
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

If you have setup your configurations in the `Aspire:Microsoft:EntityFrameworkCore:SqlServer` section you can just call the method without passing any parameter.

```cs
    builder.AddSqlServerDbContext<YourDbContext>();
```

If you want to register more than one `DbContext` with different configuration, you can to use `$"Aspire:Microsoft:EntityFrameworkCore:SqlServer:{typeof(TContext).Name}"` configuration section name. The json configuration would look like:

```json
{
  "Aspire": {
    "Microsoft": {
      "EntityFrameworkCore": {
        "SqlServer": {
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

Then calling the `AddSqlServerDbContext` method with `AnotherDbContext` type parameter would load the settings from `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:AnotherDbContext` section. 

```cs
    builder.AddSqlServerDbContext<AnotherDbContext>();
```

Also you can pass the `Action<MicrosoftEntityFrameworkCoreSqlServerSettings>` delegate to set up some or all the options inline, for example to set the `ConnectionString`:

```cs
    builder.AddSqlServerDbContext<YourDbContext>(settings => settings.ConnectionString = "YOUR_CONNECTIONSTRING");
```

Here are the configurable options with corresponding default values:

```cs
public sealed class MicrosoftEntityFrameworkCoreSqlServerSettings
{
    // The connection string of the SQL Server database to connect to. Note that this is the only option that is required to set.
    public string? ConnectionString { get; set; }

    // A boolean value that indicates whether the db context will be pooled or explicitly created every time it's requested.
    public bool DbContextPooling { get; set; } = true;
	
    // The maximum number of retry attempts. Default value is 6, set it to 0 to disable the retry mechanism.
    public int MaxRetryCount { get; set; } = 6;

    // A boolean value that indicates whether the database health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = true;
	
    // A boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    public bool Metrics { get; set; } = true;
	
    // The time in seconds to wait for the command to execute.
    public int? Timeout { get; set; }
}
```

After adding a `YourDbContext` to the builder you can get the `YourDbContext` instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
