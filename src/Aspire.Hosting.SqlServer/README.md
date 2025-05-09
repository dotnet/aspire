# Aspire.Hosting.SqlServer library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a SQL Server database resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire SQL Server Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.SqlServer
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a SQL Server resource and consume the connection using the following methods:

```csharp
var db = builder.AddSqlServer("sql").AddDatabase("db")

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/database/sql-server-component
https://learn.microsoft.com/dotnet/aspire/database/sql-server-entity-framework-component

## Feedback & contributing

https://github.com/dotnet/aspire
