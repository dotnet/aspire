# Aspire.SqlServer.EntityFrameworkCore.SqlClient library

Registers 'Scoped' [EntityFrameworkCore](https://learn.microsoft.com/en-us/ef/core/) `DbContext` service for connecting Azure SQL, MS SQL server database using [SqlClient](https://learn.microsoft.com/dotnet/api/microsoft.data.sqlclient). Configures the connection pooling, health check, logging and telemetry for the [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext).

## Getting started

### Prerequisites

- You need Azure SQL or MS SQL server database and connection string for accessing the database.

### Install the package

Install the Aspire SQL Server EntityFrameworkCore SqlClient library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.SqlServer.EntityFrameworkCore.SqlClient
```

## Usage Example

Call `AddSqlServerEntityFrameworkDBContext` extension method to add the `DbContext`  with the desired configurations exposed with `MicrosoftEntityFrameworkCoreSqlServerSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It can load the configuration options from your `*.json` settings file, by default the section name is `Aspire.SqlServer.EntityFrameworkCore.SqlClient`, you can change the configuration section name by passing the optional parameter `configurationSectionName`. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire.SqlServer.EntityFrameworkCore.SqlClient": {
      "ConnectionString": "YOUR_CONNECTIONSTRING",
      "DbContextPooling": true,
      "HealthChecks": false,
      "Tracing": false,
      "Metrics": true
  }
}
```

 If you already setup your configurations with the default name you can just call the method without passing any parameter.

```cs
    builder.AddSqlServerEntityFrameworkDBContext<YourDbContext>();
```

Otherwise call the `AddSqlServerEntityFrameworkDBContext` method with the `configurationSectionName` you have. Also you can pass the `Action<MicrosoftEntityFrameworkCoreSqlServerSettings>` delegate to set up some or all the options inline, for example to set the `ConnectionString`:

```cs
    builder.AddSqlServerEntityFrameworkDBContext<YourDbContext>(config => config.ConnectionString = "YOUR_CONNECTIONSTRING");
```

After adding the `YourDbContext` to the builder you can get a scoped `YourDbContext` instance using DI.

Here are the configurable options with corresponding default values:

```cs
public sealed class MicrosoftEntityFrameworkCoreSqlServerSettings
{
    // Gets or sets the connection string of the SQL Server database to connect to. Note that this is the only option that is required to set.
    public string? ConnectionString { get; set; }

    // Gets or sets a boolean value that indicates whether the db context will be pooled or explicitly created every time it's requested.
    public bool DbContextPooling { get; set; } = true;
	
    // Gets or sets the maximum number of retry attempts. Default value is 6, set it to 0 to disable the retry mechanism.
    public int MaxRetryCount { get; set; } = 6;

    // Gets or sets a boolean value that indicates whether the DbContext health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = true;
	
    // Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    public bool Metrics { get; set; } = true;
	
    // The time in seconds to wait for the command to execute.
    public int? Timeout { get; set; }
}
```

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
