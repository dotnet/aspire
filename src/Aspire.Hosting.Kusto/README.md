# Aspire.Hosting.Kusto library

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure a Kusto emulator resource.

## Getting started

### Install the package

In your AppHost project, install the .NET Aspire Kusto Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Kusto
```

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add a Kusto resource and consume the connection using the following method:

```csharp
var db = builder.AddKusto().RunAsEmulator();

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(db);
```

The `WithCreationScript` method configures a script to run when the Kusto emulator starts. This can be used to create databases, tables, and ingest data:

```csharp
var kusto = builder.AddKusto()
    .RunAsEmulator()
    .WithCreationScript(".create database TestDb volatile;")
    .WithCreationScript(
    """
    .execute database script with (ThrowOnErrors=true) <|
        .create-merge table TestTable (Id: int, Name: string, Timestamp: datetime)

        .ingest inline into table TestTable <|
            1,"Alice",datetime(2024-01-01T10:00:00Z)
            2,"Bob",datetime(2024-01-01T11:00:00Z)
            3,"Charlie",datetime(2024-01-01T12:00:00Z)
    """,
    "TestDb");
```

## Additional documentation

* https://learn.microsoft.com/en-us/kusto/
* https://learn.microsoft.com/en-us/kusto/api/
* https://learn.microsoft.com/en-us/azure/data-explorer/kusto-emulator-overview

## Feedback & contributing

https://github.com/dotnet/aspire
