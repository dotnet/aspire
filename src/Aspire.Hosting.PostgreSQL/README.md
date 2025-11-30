# Aspire.Hosting.PostgreSQL library

Provides extension methods and resource definitions for an Aspire AppHost to configure a PostgreSQL resource.

## Getting started

### Install the package

In your AppHost project, install the Aspire PostgreSQL Hosting library with [NuGet](https://www.nuget.org):

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

## Connection Properties

When you reference a PostgreSQL resource using `WithReference`, the following connection properties are made available to the consuming project:

### PostgreSQL server

The PostgreSQL server resource exposes the following connection properties:

| Property Name | Description |
|---------------|-------------|
| `Host` | The hostname or IP address of the PostgreSQL server |
| `Port` | The port number the PostgreSQL server is listening on |
| `Username` | The username for authentication |
| `Password` | The password for authentication |
| `Uri` | The connection URI in postgresql:// format, with the format `postgresql://{Username}:{Password}@{Host}:{Port}` |
| `JdbcConnectionString` | JDBC-format connection string, with the format `jdbc:postgresql://{Host}:{Port}`. User and password credentials are provided as separate `Username` and `Password` properties. |

### PostgreSQL database

The PostgreSQL database resource inherits all properties from its parent `PostgresServerResource` and adds:

| Property Name | Description |
|---------------|-------------|
| `Uri` | The connection URI with the database name, with the format `postgresql://{Username}:{Password}@{Host}:{Port}/{DatabaseName}` |
| `JdbcConnectionString` | JDBC connection string with database name, with the format `jdbc:postgresql://{Host}:{Port}/{DatabaseName}`. User and password credentials are provided as separate `Username` and `Password` properties. |
| `DatabaseName` | The name of the database |

Aspire exposes each property as an environment variable named `[RESOURCE]_[PROPERTY]`. For instance, the `Uri` property of a resource called `db1` becomes `DB1_URI`.

## MCP (Model Context Protocol) Support

The PostgreSQL hosting integration provides support for adding an MCP sidecar container that enables AI agents to interact with PostgreSQL databases. This is enabled by calling `WithPostgresMcp()` on the PostgreSQL server resource.

```csharp
var pg = builder.AddPostgres("pg");
pg.WithPostgresMcp();
```

The PostgreSQL MCP server (powered by [Postgres MCP Pro](https://github.com/crystaldba/postgres-mcp)) provides the following tools:

| Tool | Description |
|------|-------------|
| `list_schemas` | Lists all database schemas available in the PostgreSQL instance |
| `list_objects` | Lists database objects (tables, views, sequences, extensions) within a schema |
| `get_object_details` | Provides information about a specific database object (columns, constraints, indexes) |
| `execute_sql` | Executes SQL statements on the database |
| `explain_query` | Gets the execution plan for a SQL query, including support for hypothetical indexes |
| `get_top_queries` | Reports the slowest SQL queries based on execution time |
| `analyze_workload_indexes` | Analyzes database workload and recommends optimal indexes |
| `analyze_query_indexes` | Analyzes specific SQL queries and recommends indexes |
| `analyze_db_health` | Performs comprehensive health checks (buffer cache, connections, indexes, vacuum health) |

## Additional documentation

https://learn.microsoft.com/dotnet/aspire/database/postgresql-component
https://learn.microsoft.com/dotnet/aspire/database/postgresql-entity-framework-component

## Feedback & contributing

https://github.com/dotnet/aspire

_*Postgres, PostgreSQL and the Slonik Logo are trademarks or registered trademarks of the PostgreSQL Community Association of Canada, and used with their permission._
