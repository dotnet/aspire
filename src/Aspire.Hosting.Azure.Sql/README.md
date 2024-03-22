# Aspire.Hosting.Azure.SqlServer library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure SQL Server.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

Install the .NET Aspire Azure SQL Server Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.SqlServer
```

## Usage example

In the _Program.cs_ file of `AppHost`, register a SqlServer database and consume the connection using the following methods:

```csharp
var sql = builder.AddSqlServer("sql")
                 .AsAzureSqlDatabase()
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
