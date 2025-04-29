# Aspire.Hosting.Azure.Sql library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure SQL Server.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure SQL Server Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Sql
```

## Configure Azure Provisioning for local development

Adding Azure resources to the .NET Aspire application model will automatically enable development-time provisioning
for Azure resources so that you don't need to configure them manually. Provisioning requires a number of settings
to be available via .NET configuration. Set these values in user secrets in order to allow resources to be configured
automatically.

```json
{
    "Azure": {
      "SubscriptionId": "<your subscription id>",
      "ResourceGroupPrefix": "<prefix for the resource group>",
      "Location": "<azure location>"
    }
}
```

> NOTE: Developers must have Owner access to the target subscription so that role assignments
> can be configured for the provisioned resources.

## Usage example

In the _AppHost.cs_ file of `AppHost`, register a SqlServer database and consume the connection using the following methods:

```csharp
var sql = builder.AddAzureSqlServer("sql")
                 .AddDatabase("sqldata");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(sql);
```

The `WithReference` method configures a connection in the `MyService` project named `sqldata`. In the _Program.cs_ file of `MyService`, the sql connection can be consumed using the client library [Aspire.Microsoft.Data.SqlClient](https://www.nuget.org/packages/Aspire.Microsoft.Data.SqlClient):

```csharp
builder.AddSqlServerClient("sqldata");
```

## Additional documentation

* https://learn.microsoft.com/dotnet/framework/data/adonet/sql/
* https://learn.microsoft.com/dotnet/api/system.data.sqlclient.sqlconnection
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
