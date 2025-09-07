# Aspire.Hosting.MySql library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a MySQL resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire MySQL Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.MySql
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a MySQL resource and consume the connection using the following methods:

```csharp
var db = builder.AddMySql("mysql").AddDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/database/mysql-component

## Feedback & contributing

https://github.com/dotnet/aspire
