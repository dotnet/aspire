# Aspire.Hosting.MongoDB library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a MongoDB resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire MongoDB Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.MongoDB
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a MongoDB resource and consume the connection using the following methods:

```csharp
var db = builder.AddMongoDB("mongodb").AddDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/database/mongodb-component

## Feedback & contributing

https://github.com/dotnet/aspire
