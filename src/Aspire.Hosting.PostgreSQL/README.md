# Aspire.Hosting.PostgreSQL library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a PostgreSQL resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire PostgreSQL Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.PostgreSQL
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a PostgreSQL resource and consume the connection using the following methods:

```csharp
var db = builder.AddPostgres("pgsql").AddDatabase("mydb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

## Additional documentation
https://learn.microsoft.com/dotnet/aspire/database/postgresql-component
https://learn.microsoft.com/dotnet/aspire/database/postgresql-entity-framework-component

## Feedback & contributing

https://github.com/dotnet/aspire

_*Postgres, PostgreSQL and the Slonik Logo are trademarks or registered trademarks of the PostgreSQL Community Association of Canada, and used with their permission._
