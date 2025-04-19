# Aspire.Hosting.Azure.Sql library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure SQL DB.

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

In the _Program.cs_ file of `AppHost`, register a SqlServer database and consume the connection using the following methods:

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

## Azure SQL DB defaults

Unless otherwise specified, the Azure SQL DB created will be a 2vCores General Purpose Serverless database (GP_S_Gen5_2) with the free offer enabled.

Read more about the free offer here: [Deploy Azure SQL Database for free](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer?view=azuresql)

The free offer is configured so that when the maximum usage limit is reached, the database is stopped to avoid incurring in unexpected costs.

If you want don't want to use the free offer and instead deploy the database with the service level of your choice, specify the SKU name when adding the database resource:

```csharp
var sql = builder.AddAzureSqlServer("sql")
                 .AddDatabase("db", "my-db-name", "HS_Gen5_2");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(sql);
```

## Additional documentation

* https://learn.microsoft.com/dotnet/framework/data/adonet/sql/
* https://learn.microsoft.com/dotnet/api/system.data.sqlclient.sqlconnection
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
