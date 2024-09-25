# Aspire.Hosting.SurrealDb library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a SurrealDB database resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire SurrealDB Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.SurrealDb
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a SurrealDB resource and consume the connection using the following methods:

```csharp
var db = builder.AddSurrealServer("surreal")
                .AddNamespace("ns")
                .AddDatabase("db");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation

https://learn.microsoft.com/dotnet/aspire/database/surrealdb-component

## Feedback & contributing

https://github.com/dotnet/aspire
