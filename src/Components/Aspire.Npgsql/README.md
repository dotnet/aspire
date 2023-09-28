# Aspire.Npgsql library

Registers [NpgsqlDataSource](https://www.npgsql.org/doc/api/Npgsql.NpgsqlDataSource.html) in the DI container for connecting PostgreSQL database. Enables corresponding health check, metrics, logging and telemetry..

## Getting started

### Prerequisites

- PostgreSQL database and connection string for accessing the database.

### Install the package

Install the Aspire PostgreSQL Npgsql library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Npgsql
```

## Usage Example

Call `AddNpgsqlDataSource` extension method to add the `NpgsqlDataSource` with the desired configurations exposed with `NpgsqlSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `NpgsqlSettings` from configuration by using `Aspire:Npgsql` key. Example `appsettings.json` that configures some of the settings. note that `ConnectionString` is required to be set:

```json
{
  "Aspire": {
    "Npgsql": {
      "ConnectionString": "YOUR_CONNECTIONSTRING",
      "Metrics": false
    }
  }
}
```

If you have setup your configurations in the `Aspire:Npgsql` section you can just call the method without passing any parameter.

```cs
    builder.AddNpgsqlDataSource();
```

If you want to add more than one [NpgsqlDataSource](https://www.npgsql.org/doc/api/Npgsql.NpgsqlDataSource.html) you could use named instances. The json configuration would look like: 

```json
{
  "Aspire": {
    "Npgsql": {
      "INSTANCE_NAME": {
        "ServiceUri": "YOUR_URI",
        "HealthChecks": false
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddNpgsqlDataSource` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddNpgsqlDataSource("INSTANCE_NAME");
```

Also you can pass the `Action<NpgsqlSettings>` delegate to set up some or all the options inline, for example to turn off the `Metrics`:

```cs
    builder.AddNpgsqlDataSource(settings => settings.Metrics = true);
```

Here is the configurable options with corresponding default values:

```cs
public sealed class NpgsqlSettings
{
    // The connection string of the SQL Server database to connect to. Note that this is the only option that is required to set.
    public string? ConnectionString { get; set; }

    // A boolean value that indicates whether the DbContext health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = true;
	
    // A boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    public bool Metrics { get; set; } = true;
}
```

After adding a `NpgsqlDataSource` you can get the scoped [NpgsqlDataSource](https://learn.microsoft.com/dotnet/api/microsoft.data.sqlclient.sqlconnection) instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
