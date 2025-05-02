# Aspire.Hosting.Oracle library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure an Oracle database resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire PostgreSQL Oracle library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Oracle
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Oracle database resource and consume the connection using the following methods:

```csharp
var db = builder.AddOracle("oracle").AddDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/database/oracle-entity-framework-component?tabs=dotnet-cli

## Feedback & contributing

https://github.com/dotnet/aspire
