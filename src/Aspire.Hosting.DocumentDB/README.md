# Aspire.Hosting.DocumentDB library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a DocumentDB resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire DocumentDB Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.DocumentDB
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a DocumentDB resource and consume the connection using the following methods:

```csharp
var db = builder.AddDocumentDB("DocumentDB").AddDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation
-----TODO-------
//https://learn.microsoft.com/dotnet/aspire/database/DocumentDB-component

## Feedback & contributing

https://github.com/dotnet/aspire
