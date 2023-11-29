# Aspire.Oracle.ManagedDataAccess.Core library

Registers [OracleConnection](https://docs.oracle.com/en/database/oracle/oracle-database/23/odpnt/OracleConnectionClass.html) in the DI container for connecting Oracle® database. Enables corresponding health check and telemetry.

## Getting started

### Prerequisites

- Oracle database and connection string for accessing the database.

### Install the package

Install the .NET Aspire Oracle Managed Data Access Core library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Oracle.ManagedDataAccess.Core
```

## Usage example

In the _Program.cs_ file of your project, call the `AddOracleManagedDataAccessCore` extension method to register a `OracleConnection` for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddOracleManagedDataAccessCore("oracledb");
```

You can then retrieve the `OracleConnection` instance using dependency injection. For example, to retrieve the connection from a Web API controller:

```csharp
private readonly OracleConnection _connection;

public ProductsController(OracleConnection connection)
{
    _connection = connection;
}
```

## Configuration

The .NET Aspire Oracle Managed Data Access Core component provides multiple options to configure the database connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddOracleManagedDataAccessCore()`:

```csharp
builder.AddOracleManagedDataAccessCore("myConnection");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "myConnection": "user id=system;password=password;data source=localhost:port/freepdb1"
  }
}
```

### Use configuration providers

The .NET Aspire Oracle Managed Data Access Core component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `OracleManagedDataAccessCoreSettings` from configuration by using the `Aspire:Oracle:ManagedDataAccess:Core` key. Example `appsettings.json` that configures some of the options:

```json
{
  "Aspire": {
    "Oracle": {
      "ManagedDataAccess": {
        "Core": {
          "HealthChecks": true,
          "Tracing": true
        }
      }
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<OracleManagedDataAccessCoreSettings> configureSettings` delegate to set up some or all the options inline, for example to disable health checks from code:

```csharp
    builder.AddOracleManagedDataAccessCore("oracledb", settings => settings.HealthChecks = false);
```

## AppHost extensions

In your AppHost project, register a Oracle Database container and consume the connection using the following methods:

```csharp
var oracledb = builder.AddOracleDatabaseContainer("orcl").AddDatabase("freepdb1");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(oracledb);
```

The `WithReference` method configures a connection in the `MyService` project named `oracledb`. In the _Program.cs_ file of `MyService`, the database connection can be consumed using:

```csharp
builder.AddOracleManagedDataAccessCore("oracledb");
```

## Additional documentation

* https://github.com/oracle/dotnet-db-samples/tree/master
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
