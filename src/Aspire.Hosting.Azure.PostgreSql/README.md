# Aspire.Hosting.Azure.PostgreSQL library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure Azure Database for PostgreSQL.

## Getting started

### Prerequisites

- Azure subscription - [create one for free](https://azure.microsoft.com/free/)

### Install the package

In your AppHost project, install the .NET Aspire Azure PostgreSQL Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Azure.PostgreSQL
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, register a Postgres database and consume the connection using the following methods:

```csharp
var postgresdb = builder.AddPostgres("pg").
                        .AsAzurePostgresFlexibleServer()
                        .AddDatabase("postgresdb");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(postgresdb);
```

The `WithReference` method configures a connection in the `MyService` project named `postgresdb`. In the _Program.cs_ file of `MyService`, the database connection can be consumed using the client library [Aspire.Npgsql](https://www.nuget.org/packages/Aspire.Npgsql):

```csharp
builder.AddNpgsqlDataSource("postgresdb");
```

## Additional documentation

* https://www.npgsql.org/doc/basic-usage.html
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

_*Postgres, PostgreSQL and the Slonik Logo are trademarks or registered trademarks of the PostgreSQL Community Association of Canada, and used with their permission._
