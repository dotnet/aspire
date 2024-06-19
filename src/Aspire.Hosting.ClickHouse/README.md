# Aspire.Hosting.ClickHouse library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a ClickHouse resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire ClickHouse Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.ClickHouse
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a ClickHouse resource and consume the connection using the following methods:

```csharp
var db = builder.ClickHouse("clickhouse").AddDatabase("default");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation


## Feedback & contributing

https://github.com/dotnet/aspire
