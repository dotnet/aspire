# Aspire.Microsoft.Data.SqlClient library

Registers 'Scoped' [Microsoft.Data.SqlClient.SqlConnection](https://learn.microsoft.com/dotnet/api/microsoft.data.sqlclient.sqlconnection) factory in the DI container for connecting Azure SQL, MS SQL database. Enables health check, metrics and telemetry.

## Getting started

### Prerequisites

- Azure SQL or MS SQL server database and the connection string for accessing the database.

### Install the package

Install the Aspire SQL Server SqlClient library with [NuGet][nuget]:

```dotnetcli
dotnet add package Aspire.Microsoft.Data.SqlClient
```

## Usage Example

 Call `AddSqlServerClient` extension method to add the `SqlConnection` config with the desired configurations exposed with `MicrosoftDataSqlClientSettings`. The library supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `MicrosoftDataSqlClientSettings` from configuration by using `Aspire:Microsoft:Data:SqlClient` key. Example `appsettings.json` that configures some of the settings, note that `ConnectionString` is required to be set:

```json
{
  "Aspire": {
    "Microsoft": {
      "Data": {
        "SqlClient": {
          "ConnectionString": "YOUR_CONNECTIONSTRING",
          "HealthChecks": true,
          "Metrics": false
        }
      }
    }
  }
}
```

If you have setup your configurations in the `Aspire:Microsoft:Data:SqlClient` section you can just call the method without passing any parameter.

```cs
    builder.AddSqlServerClient();
```

If you want to add more than one [SqlConnection](https://learn.microsoft.com/dotnet/api/azure.storage.queues.queueserviceclient) you could use named instances. The json configuration would look like: 

```json
{
  "Aspire": {
    "Microsoft": {
      "Data": {
        "SqlClient": {
          "INSTANCE_NAME": {
            "ServiceUri": "YOUR_URI",
            "HealthChecks": false
          }
        }
      }
    }
  }
}
```

To load the named configuration section from the json config call the `AddSqlServerClient` method by passing the `INSTANCE_NAME`.

```cs
    builder.AddSqlServerClient("INSTANCE_NAME");
```

Also you can pass the `Action<MicrosoftDataSqlClientSettings>` delegate to set up some or all the options inline, for example to turn off the `Metrics`:

```cs
    builder.AddSqlServerClient(settings => settings.Metrics = false);
```

Here are the configurable options with corresponding default values:

```cs
public sealed class MicrosoftDataSqlClientSettings
{
    // The connection string of the SQL Server database to connect to.
    public string? ConnectionString { get; set; }

    // A boolean value that indicates whether the database health check is enabled or not.
    public bool HealthChecks { get; set; } = true;

    // A boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    public bool Tracing { get; set; } = true;
	
    // A boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    public bool Metrics { get; set; } = true;
}
```

After adding a `SqlConnection` you can get the scoped [SqlConnection](https://learn.microsoft.com/dotnet/api/microsoft.data.sqlclient.sqlconnection) instance using DI.

## Additional documentation

https://github.com/dotnet/astra/tree/main/src/Components/README.md

## Feedback & Contributing

https://github.com/dotnet/astra
